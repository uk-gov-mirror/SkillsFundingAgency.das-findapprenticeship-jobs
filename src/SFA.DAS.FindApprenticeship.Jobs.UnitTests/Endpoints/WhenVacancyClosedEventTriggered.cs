using Esfa.Recruit.Vacancies.Client.Domain.Events;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;
public class WhenVacancyClosedEventTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Command_Will_Be_Handled(
    VacancyClosedEvent command,
    [Frozen] Mock<IVacancyClosedHandler> handler,
    HandleVacancyClosedEvent sut)
    {
        await sut.Handle(command, It.IsAny<IMessageHandlerContext>());

        handler.Verify(
            x => x.Handle(
                It.Is<VacancyClosedEvent>(c => c.VacancyId == command.VacancyId)),
            Times.Once());
    }
}
