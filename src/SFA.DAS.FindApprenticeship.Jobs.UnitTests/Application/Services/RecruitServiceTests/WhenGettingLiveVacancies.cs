using FluentAssertions.Execution;
using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Alerting;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Services.RecruitServiceTests;
public class WhenGettingLiveVacancies
{
    [Test, MoqAutoData]
    public async Task Then_The_Api_Is_Called_And_Live_Vacancies_Returned(
        int pageSize,
        int pageNo,
        GetLiveVacanciesApiResponse response,
        [Frozen] Mock<IOuterApiClient> apiClient,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        FindApprenticeshipJobsService service)
    {
        response.PageNo = pageNo;
        response.PageSize = pageSize;
        
        apiClient.Setup(x =>
        x.Get<GetLiveVacanciesApiResponse>(
            It.Is<GetLiveVacanciesApiRequest>(c => c.GetUrl.Contains($"livevacancies?pageSize={pageSize}&pageNo={pageNo}"))))
            .ReturnsAsync(new ApiResponse<GetLiveVacanciesApiResponse>(response, HttpStatusCode.OK,""));

        var actual = await service.GetLiveVacancies(pageNo, pageSize);

        using (new AssertionScope())
        {
            actual.Should().BeEquivalentTo(response);
            actual.PageSize.Should().Be(pageSize);
            actual.PageNo.Should().Be(pageNo);
            indexingAlertsManager.Verify(x=>x.SendFaaImportAlertAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
    [Test, MoqAutoData]
    public void Then_The_Api_Is_Called_And_If_Error_Exception_Is_Returned(
        int pageSize,
        int pageNo,
        [Frozen] Mock<IOuterApiClient> apiClient,
        [Frozen] Mock<IIndexingAlertsManager> indexingAlertsManager,
        FindApprenticeshipJobsService service)
    {
        apiClient.Setup(x =>
                x.Get<GetLiveVacanciesApiResponse>(
                    It.Is<GetLiveVacanciesApiRequest>(c => c.GetUrl.Contains($"livevacancies?pageSize={pageSize}&pageNo={pageNo}"))))
            .ReturnsAsync(new ApiResponse<GetLiveVacanciesApiResponse>(null!, HttpStatusCode.InternalServerError, "response-error"));

        Assert.ThrowsAsync<HttpRequestException>(async() => await service.GetLiveVacancies(pageNo, pageSize));
        indexingAlertsManager.Verify(x=>x.SendFaaImportAlertAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
