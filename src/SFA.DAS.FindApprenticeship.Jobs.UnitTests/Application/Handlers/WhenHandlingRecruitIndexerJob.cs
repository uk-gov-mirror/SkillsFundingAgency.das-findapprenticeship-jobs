using AutoFixture.NUnit4;
using FluentAssertions.Execution;
using SFA.DAS.FindApprenticeship.Jobs.Application;
using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Documents;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Alerting;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers;

public class WhenHandlingRecruitIndexerJob
{
    [TestCase("\"OneLocation\"", AvailableWhere.OneLocation)]
    [TestCase("\"MultipleLocations\"", AvailableWhere.MultipleLocations)]
    [TestCase("\"AcrossEngland\"", AvailableWhere.AcrossEngland)]
    [TestCase("null", null)]
    public void Then_EmploymentLocationOption_Can_Be_Deserialized(string? value, AvailableWhere? expected)
    {
        // arrange
        var json = $"{{\"EmploymentLocationOption\":{value}}}";

        // act
        var newLiveVacancy = System.Text.Json.JsonSerializer.Deserialize<LiveVacancy>(json);

        // assert
        newLiveVacancy.Should().NotBeNull();
        newLiveVacancy!.EmploymentLocationOption.Should().Be(expected);
    }

    [Test, MoqAutoData]
    public async Task Then_The_LiveVacancies_Are_Retrieved_And_Index_Is_Created(
        List<LiveVacancy> liveVacancies,
        List<NhsVacancy> nhsLiveVacancies,
        List<CsjVacancy> civilServiceLiveVacancies,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IDateTimeService> dateTimeService,
        DateTime currentDateTime,
        RecruitIndexerJobHandler sut)
    {
        dateTimeService.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);

        var expectedIndexName = $"{Constants.IndexPrefix}{currentDateTime.ToString(Constants.IndexDateSuffixFormat)}";

        var liveVacanciesApiResponse = new GetLiveVacanciesApiResponse
        {
            Vacancies = liveVacancies,
            PageNo = 1,
            PageSize = liveVacancies.Count,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1
        };

        var nhsLiveVacanciesApiResponse = new GetNhsLiveVacanciesApiResponse
        {
            Vacancies = nhsLiveVacancies,
            PageNo = 1,
            PageSize = liveVacancies.Count,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1
        };

        var civilServiceLiveVacanciesApiResponse = new GetCivilServiceLiveVacanciesApiResponse
        {
            Vacancies = civilServiceLiveVacancies,
            PageNo = 1,
            PageSize = liveVacancies.Count,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1
        };

        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancies(It.IsAny<int>(), 500, null)).ReturnsAsync(liveVacanciesApiResponse);
        findApprenticeshipJobsService.Setup(x => x.GetNhsLiveVacancies()).ReturnsAsync(nhsLiveVacanciesApiResponse);
        findApprenticeshipJobsService.Setup(x => x.GetCivilServiceLiveVacancies()).ReturnsAsync(civilServiceLiveVacanciesApiResponse);
        azureSearchHelper.Setup(x => x.CreateIndex(It.IsAny<string>())).Returns(Task.CompletedTask);
        azureSearchHelper.Setup(x => x.UploadDocuments(It.IsAny<string>(), It.IsAny<List<ApprenticeAzureSearchDocument>>())).Returns(Task.CompletedTask);
        azureSearchHelper.Setup(x => x.UpdateAlias(Constants.AliasName, expectedIndexName)).Returns(Task.CompletedTask);

        await sut.Handle();

        using (new AssertionScope())
        {
            findApprenticeshipJobsService.Verify(x => x.GetLiveVacancies(It.IsAny<int>(), 500, null), Times.Exactly(liveVacanciesApiResponse.TotalPages));
            findApprenticeshipJobsService.Verify(x => x.GetNhsLiveVacancies(), Times.Exactly(nhsLiveVacanciesApiResponse.TotalPages));
            findApprenticeshipJobsService.Verify(x => x.GetCivilServiceLiveVacancies(), Times.Exactly(1));
            azureSearchHelper.Verify(x => x.CreateIndex(expectedIndexName), Times.Once());
            azureSearchHelper.Verify(x => x.UploadDocuments(expectedIndexName, It.IsAny<List<ApprenticeAzureSearchDocument>>()), Times.Exactly(3));
            azureSearchHelper.Verify(x => x.UpdateAlias(Constants.AliasName, expectedIndexName), Times.Once);
        }
    }
    
    
    [Test, MoqAutoData]
    public void Then_If_Error_With_The_LiveVacancies_Index_Alias_Is_Not_Updated(
        LiveVacancy vacancy,
        List<LiveVacancy> liveVacancies,
        List<NhsVacancy> nhsLiveVacancies,
        List<CsjVacancy> civilServiceLiveVacancies,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IDateTimeService> dateTimeService,
        DateTime currentDateTime,
        RecruitIndexerJobHandler sut)
    {
        dateTimeService.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);
        var expectedIndexName = $"{Constants.IndexPrefix}{currentDateTime.ToString(Constants.IndexDateSuffixFormat)}";

        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancies(It.IsAny<int>(), 500, null)).ThrowsAsync(new Exception("Errors"));
        
        Assert.ThrowsAsync<Exception>(async() => await sut.Handle());

        using (new AssertionScope())
        {
            azureSearchHelper.Verify(x => x.CreateIndex(expectedIndexName), Times.Once());
            azureSearchHelper.Verify(x => x.UpdateAlias(Constants.AliasName, expectedIndexName), Times.Never);
        }
    }

    [Test, MoqAutoData]
    public async Task Then_LiveVacancies_Is_Null_And_Index_Is_Not_Created(
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        RecruitIndexerJobHandler sut)
    {
        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancies(It.IsAny<int>(), 500, null)).ReturnsAsync(() => null);
        findApprenticeshipJobsService.Setup(x => x.GetNhsLiveVacancies()).ReturnsAsync(() => null);
        findApprenticeshipJobsService.Setup(x => x.GetCivilServiceLiveVacancies()).ReturnsAsync(() => null);

        await sut.Handle();

        using (new AssertionScope())
        {
            azureSearchHelper.Verify(x => x.CreateIndex(It.IsAny<string>()), Times.Once());
            azureSearchHelper.Verify(x => x.UploadDocuments(It.IsAny<string>(), It.IsAny<List<ApprenticeAzureSearchDocument>>()), Times.Never());
            azureSearchHelper.Verify(x => x.UpdateAlias(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }


    [Test, MoqAutoData]
    public async Task Handle_Should_Add_NhsLiveVacancies_With_EnglandOnly_Address_To_BatchDocuments(
        string countryName,
        List<LiveVacancy> liveVacancies,
        NhsVacancy nhsLiveVacancy,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IDateTimeService> dateTimeService,
        DateTime currentDateTime,
        RecruitIndexerJobHandler sut)
    {
        nhsLiveVacancy.Address!.Country = countryName;

        dateTimeService.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);

        var expectedIndexName = $"{Constants.IndexPrefix}{currentDateTime.ToString(Constants.IndexDateSuffixFormat)}";

        var liveVacanciesApiResponse = new GetLiveVacanciesApiResponse
        {
            Vacancies = [],
            PageNo = 1,
            PageSize = 0,
            TotalLiveVacancies = 0,
            TotalLiveVacanciesReturned = 0,
            TotalPages = 1
        };

        var nhsLiveVacanciesApiResponse = new GetNhsLiveVacanciesApiResponse
        {
            Vacancies = new List<NhsVacancy> { nhsLiveVacancy },
            PageNo = 1,
            PageSize = liveVacancies.Count,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1
        };

        var civilServiceLiveVacanciesApiResponse = new GetCivilServiceLiveVacanciesApiResponse
        {
            Vacancies = [],
            PageNo = 1,
            PageSize = liveVacancies.Count,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1
        };

        findApprenticeshipJobsService.Setup(x => x.GetLiveVacancies(It.IsAny<int>(), It.IsAny<int>(), null)).ReturnsAsync(liveVacanciesApiResponse);
        findApprenticeshipJobsService.Setup(x => x.GetNhsLiveVacancies()).ReturnsAsync(nhsLiveVacanciesApiResponse);
        findApprenticeshipJobsService.Setup(x => x.GetCivilServiceLiveVacancies()).ReturnsAsync(civilServiceLiveVacanciesApiResponse);
        azureSearchHelper.Setup(x => x.CreateIndex(It.IsAny<string>())).Returns(Task.CompletedTask);
        azureSearchHelper.Setup(x => x.UploadDocuments(It.IsAny<string>(), It.IsAny<List<ApprenticeAzureSearchDocument>>())).Returns(Task.CompletedTask);
        azureSearchHelper.Setup(x => x.UpdateAlias(Constants.AliasName, expectedIndexName)).Returns(Task.CompletedTask);

        // Act
        await sut.Handle();

        // Assert
        azureSearchHelper.Verify(s => s.UploadDocuments(It.IsAny<string>(),
            It.Is<IEnumerable<ApprenticeAzureSearchDocument>>(docs => docs.Any(d => d.Address!.Country == Constants.EnglandOnly))), Times.Never);
    }

    [Test, MoqAutoData]
    public async Task Then_The_Index_Statistics_Are_Checked_For_Issues(
        List<LiveVacancy> liveVacancies,
        List<NhsVacancy> nhsVacancies,
        List<CsjVacancy> csjVacancies,
        ApprenticeAzureSearchDocument searchDocument,
        [Frozen] Mock<IApprenticeAzureSearchDocumentFactory> recruitDocumentFactory,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        RecruitIndexerJobHandler sut)
    {
        // arrange
        var beforeStats = new IndexStatistics(1000);
        azureSearchHelper
            .Setup(x => x.GetAliasStatisticsAsync(Constants.AliasName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beforeStats);

        var liveVacanciesResponse = new GetLiveVacanciesApiResponse
        {
            PageNo = 1,
            PageSize = 100,
            TotalLiveVacancies = liveVacancies.Count,
            TotalLiveVacanciesReturned = liveVacancies.Count,
            TotalPages = 1,
            Vacancies = liveVacancies
        };

        nhsVacancies.ForEach(x => x.Address.Country = Constants.EnglandOnly);
        var nhsVacanciesResponse = new GetNhsLiveVacanciesApiResponse
        {
            PageNo = 1,
            PageSize = 100,
            TotalLiveVacancies = nhsVacancies.Count,
            TotalLiveVacanciesReturned = nhsVacancies.Count,
            TotalPages = 1,
            Vacancies = nhsVacancies
        };

        csjVacancies.ForEach(x => x.Address.Country = Constants.EnglandOnly);
        csjVacancies.ForEach(x => x.EmploymentLocationOption = AvailableWhere.OneLocation);
        csjVacancies.ForEach(x => x.OtherAddresses = []);
        var csjVacanciesResponse = new GetCivilServiceLiveVacanciesApiResponse
        {
            PageNo = 1,
            PageSize = 100,
            TotalLiveVacancies = csjVacancies.Count,
            TotalLiveVacanciesReturned = csjVacancies.Count,
            TotalPages = 1,
            Vacancies = csjVacancies
        };

        findApprenticeshipJobsService
            .Setup(x => x.GetLiveVacancies(It.IsAny<int>(), It.IsAny<int>(), null))
            .ReturnsAsync(liveVacanciesResponse);

        findApprenticeshipJobsService
            .Setup(x => x.GetNhsLiveVacancies())
            .ReturnsAsync(nhsVacanciesResponse);

        findApprenticeshipJobsService
            .Setup(x => x.GetCivilServiceLiveVacancies())
            .ReturnsAsync(csjVacanciesResponse);

        recruitDocumentFactory.Setup(x => x.Create(It.IsAny<LiveVacancy>())).Returns([searchDocument]);
        recruitDocumentFactory.Setup(x => x.Create(It.IsAny<CsjVacancy>())).Returns([searchDocument]);

        IndexStatistics? capturedStats = null;
        indexingAlertsManager
            .Setup(x => x.VerifySnapshotsAsync(beforeStats, It.IsAny<IndexStatistics>(), It.IsAny<CancellationToken>()))
            .Callback<IndexStatistics?, IndexStatistics?, CancellationToken>((_, st, _) => { capturedStats = st; });

        // act
        await sut.Handle();

        // assert
        indexingAlertsManager.Verify(x => x.VerifySnapshotsAsync(beforeStats, It.IsAny<IndexStatistics>(), It.IsAny<CancellationToken>()), Times.Once());
        capturedStats.Should().NotBeNull();
        capturedStats.Value.DocumentCount.Should().Be(liveVacancies.Count + nhsVacancies.Count + csjVacancies.Count);
    }

    [Test, MoqAutoData]
    public async Task Then_An_Alert_Is_Raised_If_The_Nhs_Api_Does_Not_Return_Any_Vacancies(
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        RecruitIndexerJobHandler sut)
    {
        // arrange
        findApprenticeshipJobsService.Setup(x => x.GetNhsLiveVacancies()).ReturnsAsync(new GetNhsLiveVacanciesApiResponse { Vacancies = [] });

        // act
        await sut.Handle();

        // assert
        indexingAlertsManager.Verify(x => x.SendNhsApiAlertAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test, MoqAutoData]
    public async Task Then_An_Alert_Is_Raised_If_The_Nhs_Api_Returns_Null(
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        RecruitIndexerJobHandler sut)
    {
        // arrange
        findApprenticeshipJobsService.Setup(x => x.GetNhsLiveVacancies()).ReturnsAsync((GetNhsLiveVacanciesApiResponse)null!);

        // act
        await sut.Handle();

        // assert
        indexingAlertsManager.Verify(x => x.SendNhsApiAlertAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test, MoqAutoData]
    public async Task Then_An_Alert_Is_Raised_If_The_Csj_Api_Returns_Null(
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        RecruitIndexerJobHandler sut)
    {
        // arrange
        findApprenticeshipJobsService.Setup(x => x.GetCivilServiceLiveVacancies()).ReturnsAsync((GetCivilServiceLiveVacanciesApiResponse)null!);

        // act
        await sut.Handle();

        // assert
        indexingAlertsManager.Verify(x => x.SendCsjImportAlertAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}