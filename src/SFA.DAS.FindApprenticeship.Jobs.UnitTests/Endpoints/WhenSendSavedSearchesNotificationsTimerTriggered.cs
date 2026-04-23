using System.Text.Json;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Models;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints
{
    [TestFixture]
    public class WhenGetAllSavedSearchesNotificationsTimerTriggered
    {
        [Test, MoqAutoData]
        public async Task Then_The_Index_Is_Handled(
            ILogger logger,
            SavedSearchResult mockSavedSearchCandidateVacancies,
            [Frozen] Mock<IGetAllCandidatesWithSavedSearchesHandler> handler,
            SavedSearchesNotificationsTimerTrigger sut)
        {
            var mockSavedSearchQueueItem = new SavedSearchQueueItem
            {
                Payload = JsonSerializer.Serialize<SavedSearchResult>(mockSavedSearchCandidateVacancies)
            };

            handler.Setup(x => x.Handle()).ReturnsAsync([mockSavedSearchCandidateVacancies]);

            var collector = await sut.Run(It.IsAny<TimerInfo>());
    
            handler.Verify(x => x.Handle(), Times.Once());

            collector.Should().BeEquivalentTo([mockSavedSearchQueueItem]);
        }
    }
}
