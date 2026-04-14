using System.Net;
using AutoFixture.NUnit4;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Configuration;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Infrastructure.Api;

public class WhenCallingPost
{
    [Test, MoqAutoData]
    public async Task Then_The_Endpoint_Is_Called_With_Authentication_Header_And_No_Data_For_Null_Response(
        FindApprenticeshipJobsConfiguration config)
    {
        config.ApimBaseUrl = $"https://test.local/{config.ApimBaseUrl}/";
        var configMock = new Mock<IOptions<FindApprenticeshipJobsConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);
        var postTestRequest = new PostTestRequest();

        var response = new HttpResponseMessage()
        {
            Content = null,
            StatusCode = HttpStatusCode.OK
        };

        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, config.ApimBaseUrl + postTestRequest.PostUrl, config.ApimKey, HttpMethod.Post);
        var client = new HttpClient(httpMessageHandler.Object) { BaseAddress = new Uri(config.ApimBaseUrl) };
        var apiClient = new OuterApiClient(client, configMock.Object);

        var actual = await apiClient.Post<NullResponse>(postTestRequest);

        actual.Body.Should().BeNull();
        actual.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Test, AutoData]
    public async Task Then_If_It_Is_Not_Successful_Error_Content_Is_Returned(
        FindApprenticeshipJobsConfiguration config)
    {
        config.ApimBaseUrl = $"https://test.local/{config.ApimBaseUrl}/";
        var configMock = new Mock<IOptions<FindApprenticeshipJobsConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);
        var postTestRequest = new PostTestRequest();
        var response = new HttpResponseMessage
        {
            Content = new StringContent("An Error"),
            StatusCode = HttpStatusCode.BadRequest
        };

        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, config.ApimBaseUrl + postTestRequest.PostUrl, config.ApimKey, HttpMethod.Post);
        var client = new HttpClient(httpMessageHandler.Object) { BaseAddress = new Uri(config.ApimBaseUrl) };
        var apiClient = new OuterApiClient(client, configMock.Object);

        var actual = await apiClient.Post<NullResponse>(postTestRequest);

        actual.Body.Should().BeNull();
        actual.ErrorContent.Should().Be("An Error");
        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    private class PostTestRequest : IPostApiRequest
    {
        public PostTestRequest()
        {
        }
        public string PostUrl => $"test-url/post";
    }
}