using Microsoft.Extensions.Options;
using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Configuration;

namespace SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Alerting;

public interface IIndexingAlertsManager
{
    Task VerifySnapshotsAsync(IndexStatistics? oldStats, IndexStatistics? newStats, CancellationToken cancellationToken = default);
    Task SendNhsApiAlertAsync(CancellationToken cancellationToken = default);
    Task SendNhsImportAlertAsync(CancellationToken cancellationToken = default);
    Task SendCsjImportAlertAsync(CancellationToken cancellationToken = default);
    Task SendFaaImportAlertAsync(CancellationToken cancellationToken = default);
}

public class IndexingAlertsManager(
    IIndexingAlertingConfiguration config,
    FunctionEnvironment environment,
    ITeamsClient teamsClient,
    ILogger<IndexingAlertsManager> logger): IIndexingAlertsManager
{
    private const string Origin = "FAA Indexer";
    private static string Now => DateTime.UtcNow.ToString("d/M/yyyy @ HH:mm:ss");
    private AlertMessage Alert(string message) => new(Origin, environment.EnvironmentName, message, Now);

    private AlertMessage ProblemReturningFaaData => Alert("The FAA API returned an error");
    private AlertMessage IndexEmptyMessage => Alert("The index contains no documents");
    private AlertMessage NoNhsVacanciesImported => Alert("No NHS vacancies were imported");
    private AlertMessage NoNhsVacanciesReturned => Alert("The external NHS API returned no vacancies");
    private AlertMessage NoCivilServiceVacanciesReturned => Alert("The external CSJ API returned no vacancies");
    private AlertMessage IndexThresholdBreachedMessage(int value) => Alert($"A {value}% decrease in documents has been detected");

    public async Task VerifySnapshotsAsync(IndexStatistics? oldStats, IndexStatistics? newStats, CancellationToken cancellationToken = default)
    {
        try
        {
            if (oldStats is null || newStats is null)
            {
                return;
            }
            await CompareSnapshotsAsync(oldStats.Value, newStats.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured whilst trying to verify the index statistics");
        }
    }

    public async Task SendNhsApiAlertAsync(CancellationToken cancellationToken = default)
    {
        await SendAlertAsync(NoNhsVacanciesReturned, cancellationToken);
    }

    public async Task SendNhsImportAlertAsync(CancellationToken cancellationToken = default)
    {
        await SendAlertAsync(NoNhsVacanciesImported, cancellationToken);
    }

    public async Task SendCsjImportAlertAsync(CancellationToken cancellationToken = default)
    {
        await SendAlertAsync(NoCivilServiceVacanciesReturned, cancellationToken);
    }
    
    public async Task SendFaaImportAlertAsync(CancellationToken cancellationToken = default)
    {
        await SendAlertAsync(ProblemReturningFaaData, cancellationToken);
    }

    private async Task SendAlertAsync(AlertMessage alertMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogError("Alert manager is sending the following message: {Message}", alertMessage.Detail);
            var result = await teamsClient.PostMessageAsync(alertMessage, cancellationToken);
            if (!result.Ok)
            {
                logger.LogError("Posting the alert message failed. Response status code was '{StatusCode}'", result.StatusCode);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception occured whilst trying to send an alert.");
        }
    }

    private async Task CompareSnapshotsAsync(IndexStatistics before, IndexStatistics after, CancellationToken cancellationToken = default)
    {
        var countAfter = after.DocumentCount;
        if (countAfter is 0)
        {
            await SendAlertAsync(IndexEmptyMessage, cancellationToken);
            return;
        }
        
        var countBefore = before.DocumentCount;
        var diff = countAfter - countBefore;
        if (diff is not 0)
        {
            var change = countBefore is 0 ? 0 : (double)diff / countBefore * 100;
            if (change <= -config.DocumentDecreasePercentageThreshold)
            {
                await SendAlertAsync(IndexThresholdBreachedMessage(Math.Abs((int)Math.Round(change))), cancellationToken);
            }    
        }
    }
}