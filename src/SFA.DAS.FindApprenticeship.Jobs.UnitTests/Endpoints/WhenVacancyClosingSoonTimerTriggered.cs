using Esfa.Recruit.Vacancies.Client.Domain.Events;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;

public class WhenVacancyClosingSoonTimerTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Command_Will_Be_Handled_And_Items_Queued(
        VacancyClosedEvent command,
        long returnItem1,
        long returnItem2,
        [Frozen] Mock<IVacancyClosingSoonHandler> handler,
        GetVacanciesClosingSoonTimerTrigger sut)
    {
        handler.Setup(x => x.Handle(2)).ReturnsAsync(new List<long>{returnItem1});
        handler.Setup(x => x.Handle(7)).ReturnsAsync(new List<long>{returnItem2});
        
        var collector = await sut.Run(It.IsAny<TimerInfo>());

        collector.Should().BeEquivalentTo(new List<VacancyQueueItem>(new[]
        {
            new VacancyQueueItem
            {
                VacancyReference = returnItem1, DaysToExpire = 2
            },
            new VacancyQueueItem
            {
                VacancyReference = returnItem2, DaysToExpire = 7
            }
        }));
    }
}