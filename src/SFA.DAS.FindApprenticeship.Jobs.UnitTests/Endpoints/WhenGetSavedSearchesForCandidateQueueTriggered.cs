using System.Text.Json;
using AutoFixture.NUnit4;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;

[TestFixture]
public class WhenGetSavedSearchesForCandidateQueueTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Queue_Item_Is_Processed_And_Handler_Called(
        SavedSearchResult mockSavedSearchCandidateVacancies,
        SavedSearchCandidateVacancies handlerResponse,
        [Frozen] Mock<IGetGetCandidateSavedSearchHandler> handler,
        GetSavedSearchesForCandidateQueueTrigger trigger)
    {
        var mockSavedSearchQueueItem = new SavedSearchQueueItem
        {
            Payload = JsonSerializer.Serialize(mockSavedSearchCandidateVacancies)
        };
        handler.Setup(
                x => x.Handle(It.Is<SavedSearchResult>(c => 
                    c.UserId == mockSavedSearchCandidateVacancies.UserId
                    && c.Id == mockSavedSearchCandidateVacancies.Id
                    )))
            .ReturnsAsync(handlerResponse);

        var actual =await trigger.Run(mockSavedSearchQueueItem);

        actual!.Payload.Should().Be(JsonSerializer.Serialize(handlerResponse));
    }
}