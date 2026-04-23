using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api.Requests
{
    [TestFixture]
    public class WhenBuildingPostSavedSearchNotificationApiRequest
    {
        [Test, AutoData]
        public void Then_The_Request_Is_Built(SavedSearchCandidateVacancies savedSearchCandidateVacancies)
        {
            var actual = new PostSendSavedSearchNotificationApiRequest(savedSearchCandidateVacancies);

            actual.PostUrl.Should().Be("savedSearches/sendNotification");
        }
    }
}