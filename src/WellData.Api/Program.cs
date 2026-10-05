using Serilog;
using WellData.Api;
using WellData.Api.Auth;
using WellData.Api.Commands;
using WellData.Infrastructure;
using WellData.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWellDataInfrastructure(builder.Configuration);
builder.Services.AddWellDataAuthentication(builder.Configuration);
builder.Services.AddWellDataApi(builder.Configuration);

var app = builder.Build();

await DatabaseMigrator.MigrateIfConfiguredAsync(app.Services, app.Configuration, app.Lifetime.ApplicationStopping);

if (await IngestionCommands.TryRunAsync(app.Services, args, app.Lifetime.ApplicationStopping))
{
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

/// <summary>Exposed so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
