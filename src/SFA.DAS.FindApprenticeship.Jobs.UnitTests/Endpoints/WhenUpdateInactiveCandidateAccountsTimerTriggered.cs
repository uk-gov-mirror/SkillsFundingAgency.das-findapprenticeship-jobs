using AutoFixture.NUnit4;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
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
    public class WhenUpdateInactiveCandidateAccountsTimerTriggered
    {
        [Test, MoqAutoData]
        public async Task Then_The_Command_Will_Be_Handled_And_Items_Queued(
            List<Candidate> candidates,
            [Frozen] Mock<IGetDormantCandidateAccountsHandler> handler,
            UpdateInactiveCandidateAccountsTimerTrigger trigger)
        {
            var mockUnsubscribeQueueItem = new UpdateCandidateStatusQueueItem
            {
                Candidates = candidates.ToList()
            };

            handler.Setup(x => x.Handle()).ReturnsAsync(candidates);

            var collector = await trigger.Run(It.IsAny<TimerInfo>());

            handler.Verify(x => x.Handle(), Times.Once());

            collector.Should().BeEquivalentTo(mockUnsubscribeQueueItem);
        }
    }
}
