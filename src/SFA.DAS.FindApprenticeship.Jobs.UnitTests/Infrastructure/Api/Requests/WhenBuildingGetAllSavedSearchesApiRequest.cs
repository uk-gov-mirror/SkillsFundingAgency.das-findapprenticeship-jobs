using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api.Requests
{
    [TestFixture]
    public class WhenBuildingGetAllSavedSearchesApiRequest
    {
        [Test, AutoData]
        public void Then_The_Url_Is_Correctly_Built(int pageNumber, int pageSize, string lastRunDateTime, int maxApprenticeshipSearchResultCount)
        {
            var actual = new GetSavedSearchesApiRequest(pageNumber, pageSize, lastRunDateTime, maxApprenticeshipSearchResultCount);

            actual.GetUrl.Should().Be($"savedSearches?pageSize={pageSize}&pageNo={pageNumber}&lastRunDateTime={lastRunDateTime}&maxApprenticeshipSearchResultCount={maxApprenticeshipSearchResultCount}");
        }
    }
}