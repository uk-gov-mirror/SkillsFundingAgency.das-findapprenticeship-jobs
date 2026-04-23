using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers;

public class WhenHandlingGetGetCandidateSavedSearchHandler
{
    [Test, MoqAutoData]
    public async Task Then_The_Saved_Search_Results_Are_Returned_For_Each_Candidate(
        SavedSearchResult result,
        SavedSearchCandidateVacancies response,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeShipJobsService,
        GetGetCandidateSavedSearchHandler handler)
    {
        findApprenticeShipJobsService.Setup(x => x.GetSavedSearchResultsForCandidate(result)).ReturnsAsync(response);
        
        var actual = await handler.Handle(result);
        
        actual.Should().BeEquivalentTo(response);
    }
}