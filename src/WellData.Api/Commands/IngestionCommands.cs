using WellData.Core;
using WellData.Infrastructure.Ingestion.Master;
using WellData.Infrastructure.Ingestion.Volumetric;

namespace WellData.Api.Commands;

/// <summary>
/// One-off ingestion commands run from the command line instead of starting the web server:
/// "ingest-master", "ingest YYYY-MM", and "backfill N" (add "--reload" after changing the operator subset).
/// </summary>
public static class IngestionCommands
{
    /// <returns>True when the arguments named a command and it was run.</returns>
    public static async Task<bool> TryRunAsync(IServiceProvider services, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !IsCommand(args[0]))
        {
            return false;
        }

        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        switch (args)
        {
            case ["ingest-master"]:
                await provider.GetRequiredService<MasterDataIngestionService>().IngestAsync(cancellationToken);
                break;

            case ["ingest", var monthText] when ProductionMonth.TryParse(monthText, out var month):
                Report(await provider.GetRequiredService<VolumetricIngestionService>().IngestMonthAsync(month, cancellationToken));
                break;

            case ["backfill", var monthsText, .. var flags] when int.TryParse(monthsText, out var months) && months > 0 && flags is [] or ["--reload"]:
                var reload = flags is ["--reload"];
                foreach (var result in await provider.GetRequiredService<BackfillService>().BackfillAsync(months, reload, cancellationToken))
                {
                    Report(result);
                }

                break;

            default:
                Console.Error.WriteLine("Usage: ingest-master | ingest YYYY-MM | backfill <months> [--reload]");
                Environment.ExitCode = 1;
                break;
        }

        return true;
    }

    private static bool IsCommand(string argument) => argument is "ingest-master" or "ingest" or "backfill";

    private static void Report(ImportResult result)
    {
        var run = result.Run;
        Console.WriteLine(run is null
            ? $"{result.Month}: {result.Outcome}"
            : $"{result.Month}: {result.Outcome}, staged {run.RowsStaged}, loaded {run.RowsLoaded}, rejected {run.RowsRejected}, skipped {run.RowsSkipped}");

        if (result.Outcome == ImportOutcome.Failed)
        {
            Console.Error.WriteLine($"  {run?.ErrorMessage}");
            Environment.ExitCode = 1;
        }
    }
}
