using System.Net;
using AutoFixture.NUnit4;
using Moq;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Services.RecruitServiceTests;

public class WhenSendingApplicationReminder
{
    [Test, MoqAutoData]
    public async Task Then_The_Api_Is_Called_And_A_Reminder_Sent(
        long vacancyRef,
        int daysUntilClosing,
        [Frozen] Mock<IOuterApiClient> apiClient,
        FindApprenticeshipJobsService service)
    {
        apiClient.Setup(x =>
                x.Post<NullResponse>(
                    It.Is<PostSendApplicationClosingSoonRequest>(c => c.PostUrl.Contains($"livevacancies/{vacancyRef}?daysUntilClosing={daysUntilClosing}"))))
            .ReturnsAsync(new ApiResponse<NullResponse>(null,HttpStatusCode.OK,""));

        await service.SendApplicationClosingSoonReminder(vacancyRef, daysUntilClosing);

        apiClient.Verify(x =>
            x.Post<NullResponse>(
                It.Is<PostSendApplicationClosingSoonRequest>(c => c.PostUrl.Contains($"livevacancies/{vacancyRef}?daysUntilClosing={daysUntilClosing}"))), Times.Once);
    }
}