using AutoFixture.NUnit4;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Configuration;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api;

public class WhenCallingGet
{
    [Test, MoqAutoData]
    public async Task Then_The_Endpoint_Is_Called_With_Authentication_Header_And_Data_Returned(
        List<string> testObject,
        FindApprenticeshipJobsConfiguration config)
    {
        config.ApimBaseUrl = $"https://test.local/{config.ApimBaseUrl}/";
        var configMock = new Mock<IOptions<FindApprenticeshipJobsConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);
        var getTestRequest = new GetTestRequest();

        var response = new HttpResponseMessage()
        {
            Content = new StringContent(JsonConvert.SerializeObject(testObject)),
            StatusCode = HttpStatusCode.Accepted
        };

        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, config.ApimBaseUrl + getTestRequest.GetUrl, config.ApimKey, HttpMethod.Get);
        var client = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri(config.ApimBaseUrl)
        };
        var apiClient = new OuterApiClient(client, configMock.Object);

        var actual = await apiClient.Get<List<string>>(getTestRequest);

        actual.Body.Should().BeEquivalentTo(testObject);
    }

    [Test, AutoData]
    public async Task Then_If_It_Is_Not_Successful_Error_Content_Is_Returned(
    FindApprenticeshipJobsConfiguration config)
    {
        config.ApimBaseUrl = $"https://test.local/{config.ApimBaseUrl}/";
        var configMock = new Mock<IOptions<FindApprenticeshipJobsConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);
        var getTestRequest = new GetTestRequest();
        var response = new HttpResponseMessage
        {
            Content = new StringContent(""),
            StatusCode = HttpStatusCode.BadRequest
        };

        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, config.ApimBaseUrl + getTestRequest.GetUrl, config.ApimKey, HttpMethod.Get);
        var client = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri(config.ApimBaseUrl)
        };
        var apiClient = new OuterApiClient(client, configMock.Object);

        var actual = await apiClient.Get<List<string>>(getTestRequest);

        actual.Body.Should().BeNull();
        actual.ErrorContent.Should().Be(string.Empty);
    }

    [Test, AutoData]
    public async Task Then_If_It_Is_Not_Found_Default_Is_Returned(
    FindApprenticeshipJobsConfiguration config)
    {
        config.ApimBaseUrl = $"https://test.local/{config.ApimBaseUrl}/";
        var configMock = new Mock<IOptions<FindApprenticeshipJobsConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);
        var getTestRequest = new GetTestRequest();
        var response = new HttpResponseMessage
        {
            Content = new StringContent(""),
            StatusCode = HttpStatusCode.NotFound
        };

        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, config.ApimBaseUrl + getTestRequest.GetUrl, config.ApimKey, HttpMethod.Get);
        var client = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri(config.ApimBaseUrl)
        };
        var apiClient = new OuterApiClient(client, configMock.Object);

        var actual = await apiClient.Get<List<string>>(getTestRequest);

        actual.Body.Should().BeNull();
    }

    private class GetTestRequest : IGetApiRequest
    {
        public GetTestRequest()
        {
        }
        public string GetUrl => $"test-url/get";
    }

}
