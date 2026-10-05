using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Serilog;
using WellData.Api.BackgroundServices;
using WellData.Api.Json;
using WellData.Api.Services;
using WellData.Core;

namespace WellData.Api;

public static class ServiceRegistration
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddWellDataApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Structured logs: message templates keep values such as month and row counts as named properties.
        services.AddSerilog(logger => logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new ProductionMonthJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Errors are returned as RFC 7807 problem details, including unhandled exceptions and empty 4xx responses.
        services.AddProblemDetails();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Well Production Data Integrity API", Version = "v1" });
            options.MapType<ProductionMonth>(() => new OpenApiSchema { Type = JsonSchemaType.String, Pattern = @"^\d{4}-\d{2}$" });
            options.MapType<ProductionMonth?>(() => new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Pattern = @"^\d{4}-\d{2}$" });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken returned by POST /api/auth/token.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });
        });

        services.AddHostedService<ScheduledIngestionService>();

        services.AddScoped<WellQueryService>();
        services.AddScoped<FacilityQueryService>();
        services.AddScoped<ProductionQueryService>();
        services.AddScoped<IssueService>();
        services.AddScoped<RuleService>();
        services.AddScoped<ImportService>();
        services.AddScoped<KpiService>();
        services.AddScoped<CostCentreService>();

        return services;
    }
}
