using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Services.FindApprenticeshipJobsServiceTests;

public class WhenGetSavedSearchResultsForCandidate
{
    [Test, MoqAutoData]
    public async Task Then_The_Api_Is_Called_And_Results_Returned(
        SavedSearchResult request,
        SavedSearchCandidateVacancies apiResponse,
        [Frozen] Mock<IOuterApiClient> outerApiClient,
        FindApprenticeshipJobsService service)
    {
        var apiRequest = new PostGetSavedSearchResultsForCandidateRequest(request);
        outerApiClient
            .Setup(x => x.PostWithResponseCode<SavedSearchCandidateVacancies>(
                It.Is<PostGetSavedSearchResultsForCandidateRequest>(c => 
                    c.PostUrl.Equals("savedSearches/GetSavedSearchResult") &&
                    c.Data == apiRequest.Data
                    )
                )).ReturnsAsync(new ApiResponse<SavedSearchCandidateVacancies>(apiResponse, HttpStatusCode.OK,""));
        
        var actual = await service.GetSavedSearchResultsForCandidate(request);

        actual.Should().BeEquivalentTo(apiResponse);
    }
    
    [Test, MoqAutoData]
    public async Task Then_The_Api_Is_Called_And_If_Not_Found_Null_Returned(
        SavedSearchResult request,
        SavedSearchCandidateVacancies apiResponse,
        [Frozen] Mock<IOuterApiClient> outerApiClient,
        FindApprenticeshipJobsService service)
    {
        var apiRequest = new PostGetSavedSearchResultsForCandidateRequest(request);
        outerApiClient
            .Setup(x => x.PostWithResponseCode<SavedSearchCandidateVacancies>(
                It.Is<PostGetSavedSearchResultsForCandidateRequest>(c => c.PostUrl.Equals(apiRequest.PostUrl))
            )).ReturnsAsync(new ApiResponse<SavedSearchCandidateVacancies>(null!, HttpStatusCode.NotFound,""));
        
        var actual = await service.GetSavedSearchResultsForCandidate(request);

        actual.Should().BeNull();
    }
}