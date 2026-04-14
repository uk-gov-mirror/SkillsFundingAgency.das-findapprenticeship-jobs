using AutoFixture.NUnit4;
using Moq;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Candidate;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints
{
    [TestFixture]
    public class WhenUpdateInactiveCandidateAccountsQueueTriggered
    {
        [Test, MoqAutoData]
        public async Task Then_The_Queue_Item_Is_Processed_And_UnsubscribeHandler_Called(
            UpdateCandidateStatusQueueItem mockUnsubscribeQueueItem,
            [Frozen] Mock<IUpdateCandidateStatusHandler> handler,
            UpdateInactiveCandidateAccountsQueueTrigger trigger)
        {
            await trigger.Run(mockUnsubscribeQueueItem);

            handler.Verify(x => x.BatchHandle(It.IsAny<List<Candidate>>()), Times.Once());
        }
    }
}
