using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api.Requests;

public class WhenBuildingPostSendApplicationClosingSoonRequest
{
    [Test, AutoData]
    public void Then_The_Request_Is_Constructed_Correctly(long vacancyRef, int daysUntilClosing)
    {
        var actual = new PostSendApplicationClosingSoonRequest(vacancyRef, daysUntilClosing);

        actual.PostUrl.Should().Be($"livevacancies/{vacancyRef}?daysUntilClosing={daysUntilClosing}");   
    }
}