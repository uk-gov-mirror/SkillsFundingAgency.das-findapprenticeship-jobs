using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers
{
    [TestFixture]
    public class WhenHandlingSendSavedSearchNotification
    {
        [Test, MoqAutoData]
        public async Task Then_The_Notification_Is_Sent(
        SavedSearchCandidateVacancies mockSavedSearchCandidateVacancies,
        [Frozen] Mock<IBatchTaskRunner> batchTaskRunner,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeshipJobsService,
        SendSavedSearchesNotificationHandler handler)
        {
            await handler.Handle(mockSavedSearchCandidateVacancies);
            
            findApprenticeshipJobsService.Verify(
                x => x.SendSavedSearchNotification(mockSavedSearchCandidateVacancies), Times.Once());
        }
    }
}
