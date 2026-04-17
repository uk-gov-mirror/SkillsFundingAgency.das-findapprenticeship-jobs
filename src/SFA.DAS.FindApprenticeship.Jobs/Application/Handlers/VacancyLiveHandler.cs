using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;

public class VacancyLiveHandler(
    IAzureSearchHelper azureSearchHelper,
    IFindApprenticeshipJobsService findApprenticeshipJobsService,
    ILogger<VacancyLiveHandler> log,
    IApprenticeAzureSearchDocumentFactory documentFactory)
    : IVacancyLiveHandler
{
    public async Task Handle(VacancyLiveEvent vacancyLiveEvent)
    {
        log.LogInformation("Vacancy Live Event handler invoked at {DateTime}", DateTime.UtcNow);

        var alias = await azureSearchHelper.GetAlias(Domain.Constants.AliasName);
        var indexName = alias?.Indexes.FirstOrDefault();

        var liveVacancy = await findApprenticeshipJobsService.GetLiveVacancy(vacancyLiveEvent.VacancyReference);
        if (!string.IsNullOrEmpty(indexName))
        {
            var documents = documentFactory.Create(liveVacancy);
            await azureSearchHelper.UploadDocuments(indexName, documents);
        }
        else
        {
            log.LogInformation("Handle VacancyLiveEvent failed with indexName {IndexName} and vacancyId {VacancyId}", indexName, vacancyLiveEvent.VacancyId);
        }
    }
}