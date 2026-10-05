using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WellData.Core.Validation;
using WellData.Infrastructure.Auditing;
using WellData.Infrastructure.Erp;
using WellData.Infrastructure.Ingestion;
using WellData.Infrastructure.Ingestion.Master;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Periods;
using WellData.Infrastructure.Persistence;
using WellData.Infrastructure.Petrinex;
using WellData.Infrastructure.Scheduling;
using WellData.Infrastructure.Validation;

namespace WellData.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWellDataInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, SystemUser>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<WellDataDbContext>((provider, options) => options
            .UseSqlServer(configuration.GetConnectionString("WellData"))
            .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddPetrinex(configuration);

        services.AddSingleton(ValidationEngine.CreateDefault());
        services.AddScoped<SourceFileRegistry>();
        services.AddScoped<MasterDataMerger>();
        services.AddScoped<MasterDataIngestionService>();
        services.AddScoped<StagingBulkLoader>();
        services.AddScoped<VolumetricRecordBulkWriter>();
        services.AddScoped<VolumetricPromoter>();
        services.AddScoped<VolumetricIngestionService>();
        services.AddScoped<BackfillService>();

        services.AddOptions<ValidationOptions>().Bind(configuration.GetSection(ValidationOptions.SectionName));
        services.AddScoped<SqlRuleRunner>();

        services
            .AddOptions<IngestionSchedulerOptions>()
            .Bind(configuration.GetSection(IngestionSchedulerOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddScoped<ScheduledMonthChecker>();

        services.AddScoped<PeriodGuard>();
        services.AddScoped<PeriodService>();
        services
            .AddOptions<ErpExportOptions>()
            .Bind(configuration.GetSection(ErpExportOptions.SectionName))
            .PostConfigure<IHostEnvironment>((options, environment) =>
                options.Directory = Path.GetFullPath(options.Directory, environment.ContentRootPath));
        services.AddScoped<ErpExportService>();

        return services;
    }

    private static void AddPetrinex(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<PetrinexOptions>()
            .Bind(configuration.GetSection(PetrinexOptions.SectionName))
            .PostConfigure<IHostEnvironment>((options, environment) =>
                options.CacheDirectory = Path.GetFullPath(options.CacheDirectory, environment.ContentRootPath));

        services.AddSingleton(provider =>
            new OperatorSubset(provider.GetRequiredService<IOptions<PetrinexOptions>>().Value.OperatorBaIds));

        services
            .AddHttpClient<IPetrinexClient, PetrinexClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler(resilience =>
            {
                // Petrinex can take over a minute to start sending a file, far longer than the 10 second default.
                resilience.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(6);
            });
    }
}
