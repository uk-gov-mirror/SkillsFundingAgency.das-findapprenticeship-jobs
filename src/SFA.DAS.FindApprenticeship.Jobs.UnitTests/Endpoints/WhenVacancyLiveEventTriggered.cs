using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;

public class WhenVacancyLiveEventTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Command_Will_Be_Handled(
        VacancyLiveEvent command,
        [Frozen] Mock<IVacancyLiveHandler> handler,
        HandleVacancyLiveEvent sut)
    {
        // act
        await sut.Handle(command, It.IsAny<IMessageHandlerContext>());

        // assert
        handler.Verify(
            x => x.Handle(
                It.Is<VacancyLiveEvent>(c => c.VacancyId == command.VacancyId)),
            Times.Once());
    }
}
