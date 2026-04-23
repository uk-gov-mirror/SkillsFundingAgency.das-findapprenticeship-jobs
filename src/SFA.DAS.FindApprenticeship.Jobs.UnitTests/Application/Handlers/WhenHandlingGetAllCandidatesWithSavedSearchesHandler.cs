using FluentAssertions.Execution;
using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers;

[TestFixture]
public class WhenHandlingGetAllCandidatesWithSavedSearchesHandler
{
    [Test, MoqAutoData]
    public async Task Then_The_Candidates_With_Saved_Searches_Are_Returned(
        List<GetCandidateSavedSearchesApiResponse.SavedSearchResult> mockSavedSearchResult,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeShipJobsService,
        [Frozen] Mock<IDateTimeService> dateTimeService,
        DateTime currentDateTime,
        GetAllCandidatesWithSavedSearchesHandler sut)
    {
        const int pageNumber = 1;
        const int pageSize = 1000;
        const int maxApprenticeshipSearchResultCount = 5;
        const string sortOrder = "AgeDesc";

        dateTimeService.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);
        var expectedRunDate = currentDateTime.AddDays(-7);
            
            
        var mockGetSavedSearchesApiResponse = new GetCandidateSavedSearchesApiResponse
        {
            SavedSearchResults = mockSavedSearchResult,
            PageIndex = 1,
            TotalCount = mockSavedSearchResult.Count,
            PageSize = mockSavedSearchResult.Count,
            TotalPages = 1
        };

        findApprenticeShipJobsService.Setup(x =>
                x.GetSavedSearches(pageNumber, pageSize, expectedRunDate.ToString("O"), maxApprenticeshipSearchResultCount, sortOrder))
            .ReturnsAsync(mockGetSavedSearchesApiResponse);
            
        findApprenticeShipJobsService.Setup(x =>
                x.GetSavedSearches(2, pageSize, expectedRunDate.ToString("O"), maxApprenticeshipSearchResultCount, sortOrder))
            .ReturnsAsync(new GetCandidateSavedSearchesApiResponse
            {
                SavedSearchResults = [],
                PageIndex = 2,
                TotalCount = 3,
                TotalPages = 1
            });

        var result = await sut.Handle();

        using (new AssertionScope())
        {
            result.Count.Should().Be(mockSavedSearchResult.Count);
            result.Should().BeEquivalentTo(mockSavedSearchResult.Select(c=>(SavedSearchResult)c).ToList(), options=> options.Excluding(c=>c.LastRunDate));
            result.TrueForAll(c => c.LastRunDate == expectedRunDate).Should().BeTrue();
        }
    }
     [Test, MoqAutoData]
    public async Task Then_The_Candidates_With_Saved_Searches_Are_Returned_And_Adds_Multiple_Pages(
        List<GetCandidateSavedSearchesApiResponse.SavedSearchResult> mockSavedSearchResult,
        List<GetCandidateSavedSearchesApiResponse.SavedSearchResult> mockSavedSearchResult2,
        [Frozen] Mock<IFindApprenticeshipJobsService> findApprenticeShipJobsService,
        [Frozen] Mock<IDateTimeService> dateTimeService,
        DateTime currentDateTime,
        GetAllCandidatesWithSavedSearchesHandler sut)
    {
        const int pageNumber = 1;
        const int pageSize = 1000;
        const int maxApprenticeshipSearchResultCount = 5;
        const string sortOrder = "AgeDesc";

        dateTimeService.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);
        var expectedRunDate = currentDateTime.AddDays(-7);
            
            
        var mockGetSavedSearchesApiResponse = new GetCandidateSavedSearchesApiResponse
        {
            SavedSearchResults = mockSavedSearchResult,
            PageIndex = 1,
            TotalCount = mockSavedSearchResult.Count,
            PageSize = mockSavedSearchResult.Count,
            TotalPages = 3
        };

        findApprenticeShipJobsService.Setup(x =>
                x.GetSavedSearches(pageNumber, pageSize, expectedRunDate.ToString("O"), maxApprenticeshipSearchResultCount, sortOrder))
            .ReturnsAsync(mockGetSavedSearchesApiResponse);
            
        findApprenticeShipJobsService.Setup(x =>
                x.GetSavedSearches(2, pageSize, expectedRunDate.ToString("O"), maxApprenticeshipSearchResultCount, sortOrder))
            .ReturnsAsync(new GetCandidateSavedSearchesApiResponse
            {
                SavedSearchResults = mockSavedSearchResult2,
                PageIndex = 2,
                TotalCount = 6,
                TotalPages = 3
            });
        findApprenticeShipJobsService.Setup(x =>
                x.GetSavedSearches(3, pageSize, expectedRunDate.ToString("O"), maxApprenticeshipSearchResultCount, sortOrder))
            .ReturnsAsync(new GetCandidateSavedSearchesApiResponse
            {
                SavedSearchResults = [],
                PageIndex = 3,
                TotalCount = 6,
                TotalPages = 2
            });

        var result = await sut.Handle();

        using (new AssertionScope())
        {
            var results = mockSavedSearchResult;
            results.AddRange(mockSavedSearchResult2);
            result.Count.Should().Be(results.Count);
            result.Should().BeEquivalentTo(results.Select(c=>(SavedSearchResult)c).ToList(), options=> options.Excluding(c=>c.LastRunDate));
            result.TrueForAll(c => c.LastRunDate == expectedRunDate).Should().BeTrue();
        }
    }
}