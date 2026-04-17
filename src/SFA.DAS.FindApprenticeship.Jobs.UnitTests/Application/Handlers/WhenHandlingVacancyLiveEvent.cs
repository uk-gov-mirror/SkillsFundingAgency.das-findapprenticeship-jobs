using Azure;
using Azure.Search.Documents.Indexes.Models;
using SFA.DAS.FindApprenticeship.Jobs.Application;
using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Documents;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers;

[TestFixture]
public class WhenHandlingVacancyLiveEvent
{
    [Test, MoqAutoData]
    public async Task Then_The_Vacancy_Is_Uploaded_To_The_Index(
        ILogger log,
        VacancyLiveEvent vacancyLiveEvent,
        string indexName,
        int programmeId,
        Response<ApprenticeAzureSearchDocument> document,
        Response<GetLiveVacancyApiResponse> liveVacancy,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IApprenticeAzureSearchDocumentFactory> documentFactory,
        VacancyLiveHandler sut)
    {
        // arrange
        liveVacancy.Value.StandardLarsCode = programmeId;

        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancy(vacancyLiveEvent.VacancyReference)).ReturnsAsync(liveVacancy);
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName))
            .ReturnsAsync(() => new SearchAlias(Constants.AliasName, new[] { indexName }));
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName)).ReturnsAsync(() => new SearchAlias(Constants.AliasName, [indexName]));
        documentFactory.Setup(x => x.Create(liveVacancy)).Returns([document.Value]);

        // act
        await sut.Handle(vacancyLiveEvent);

        // assert
        azureSearchHelper.Verify(
            x => x.UploadDocuments(indexName, 
                It.Is<IEnumerable<ApprenticeAzureSearchDocument>>(d => d.Single() == document.Value)),
            Times.Once()
        );
    }

    [Test, MoqAutoData]
    public async Task Then_The_Event_Is_Ignored_If_No_Index_Is_Currently_Aliased(
        ILogger log,
        VacancyLiveEvent vacancyLiveEvent,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        VacancyLiveHandler sut)
    {
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName))
            .ReturnsAsync(() => null);

        await sut.Handle(vacancyLiveEvent);

        azureSearchHelper.Verify(x => x.UploadDocuments(It.IsAny<string>(), It.IsAny<IEnumerable<ApprenticeAzureSearchDocument>>()),
            Times.Never());
    }

    [Test, MoqAutoData]
    public async Task Then_The_Vacancy_With_OtherAddresses_Is_Uploaded_To_The_Index(
        List<Address> otherAddresses,
        ILogger log,
        VacancyLiveEvent vacancyLiveEvent,
        string indexName,
        int programmeId,
        GetLiveVacancyApiResponse liveVacancy,
        List<ApprenticeAzureSearchDocument> azureSearchDocuments,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IApprenticeAzureSearchDocumentFactory> azureDocumentFactory,
        VacancyLiveHandler sut)
    {
        liveVacancy.EmploymentLocations = otherAddresses;
        liveVacancy.StandardLarsCode = programmeId;

        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancy(vacancyLiveEvent.VacancyReference))
            .ReturnsAsync(liveVacancy);
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName))
            .ReturnsAsync(() => new SearchAlias(Constants.AliasName, [indexName]));
        azureDocumentFactory.Setup(x=>x.Create(liveVacancy)).Returns(azureSearchDocuments);

        await sut.Handle(vacancyLiveEvent);

        azureSearchHelper.Verify(x => x.UploadDocuments(It.Is<string>(i => i == indexName),
                azureSearchDocuments), Times.Once());
    }
}
