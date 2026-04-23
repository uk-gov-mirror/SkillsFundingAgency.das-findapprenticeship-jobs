using System.Text.Json;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Models;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;

[TestFixture]
public class WhenSavedSearchesNotificationsQueueTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Queue_Item_Is_Processed_And_SendReminderHandler_Called(
        SavedSearchCandidateVacancies mockSavedSearchCandidateVacancies,
        [Frozen] Mock<ISendSavedSearchesNotificationHandler> handler,
        SendSavedSearchesNotificationsQueueTrigger trigger)
    {
        var mockSavedSearchQueueItem = new SavedCandidateSearchResultQueueItem
        {
            Payload = JsonSerializer.Serialize<SavedSearchCandidateVacancies>(mockSavedSearchCandidateVacancies)
        };

        await trigger.Run(mockSavedSearchQueueItem);

        handler.Verify(x => x.Handle(It.IsAny<SavedSearchCandidateVacancies>()), Times.Once());
    }
}