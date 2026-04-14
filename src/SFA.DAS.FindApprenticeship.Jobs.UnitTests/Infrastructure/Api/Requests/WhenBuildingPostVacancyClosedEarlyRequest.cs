using AutoFixture.NUnit4;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api.Requests;

public class WhenBuildingPostVacancyClosedEarlyRequest
{
    [Test, AutoData]
    public void Then_The_Request_Is_Built(long vacancyRef)
    {
        var actual = new PostVacancyClosedEarlyRequest(vacancyRef);

        actual.PostUrl.Should().Be($"livevacancies/{vacancyRef}/close");
    }
}