using Esfa.Recruit.Vacancies.Client.Domain.Events;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;
public class WhenVacancyUpdatedEventTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Message_Will_Be_Handled(
        LiveVacancyUpdatedEvent message,
        [Frozen] Mock<IVacancyUpdatedHandler> handler,
        HandleVacancyUpdatedEvent sut)
    {
        await sut.Handle(message, It.IsAny<IMessageHandlerContext>());

        handler.Verify(x => x.Handle(It.Is<LiveVacancyUpdatedEvent>(c => c.VacancyId == message.VacancyId)), Times.Once());
    }
}
