using AutoFixture.NUnit4;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api.Requests;
public class WhenBuildingGetLiveVacanciesRequest
{
    [Test, AutoData]
    public void Then_The_Url_Is_Correctly_Built(int pageNo, int pageSize)
    {
        var actual = new GetLiveVacanciesApiRequest(pageNo, pageSize, null);

        actual.GetUrl.Should().Be($"livevacancies?pageSize={pageSize}&pageNo={pageNo}");
    }
    
    [Test, AutoData]
    public void Then_The_Url_Is_Correctly_Built_With_Optional_Params(int pageNo, int pageSize, DateTime closingDate)
    {
        var actual = new GetLiveVacanciesApiRequest(pageNo, pageSize, closingDate);

        actual.GetUrl.Should().Be($"livevacancies?pageSize={pageSize}&pageNo={pageNo}&closingDate={closingDate.Date}");
    }
}
