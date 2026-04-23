using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Domain;

public class WhenCastingApiResponseToSavedSearchResult
{
    [Test, AutoData]
    public void Then_The_Fields_Are_Mapped(GetCandidateSavedSearchesApiResponse.SavedSearchResult source)
    {
        var actual = (SavedSearchResult)source;

        actual.Should().BeEquivalentTo(source);
    }
}