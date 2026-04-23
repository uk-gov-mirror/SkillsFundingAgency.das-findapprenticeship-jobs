using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Services.RecruitServiceTests;

public class WhenClosingVacancyEarly
{
    [Test, MoqAutoData]
    public async Task Then_The_Api_Is_Called_With_Vacancy_Ref(
        long vacancyRef,
        [Frozen] Mock<IOuterApiClient> apiClient,
        FindApprenticeshipJobsService service)
    {
        apiClient.Setup(x =>
                x.Post<NullResponse>(
                    It.Is<PostVacancyClosedEarlyRequest>(c => c.PostUrl.Contains($"livevacancies/{vacancyRef}/close"))))
            .ReturnsAsync(new ApiResponse<NullResponse>(null,HttpStatusCode.OK,""));

        await service.CloseVacancyEarly(vacancyRef);
        
        apiClient.Verify(x =>
            x.Post<NullResponse>(
                It.Is<PostVacancyClosedEarlyRequest>(c => c.PostUrl.Contains($"livevacancies/{vacancyRef}/close"))), Times.Once());
    }
}