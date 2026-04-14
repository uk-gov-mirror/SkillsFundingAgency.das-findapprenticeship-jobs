using AutoFixture.NUnit4;
using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Moq;
using NUnit.Framework;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Endpoints;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Endpoints;
public class WhenVacancyApprovedEventTriggered
{
    [Test, MoqAutoData]
    public async Task Then_The_Command_Will_Be_Handled(
        VacancyApprovedEvent command,
        [Frozen] Mock<IVacancyApprovedHandler> handler,
        HandleVacancyApprovedEvent sut)
    {
        await sut.Handle(command, It.IsAny<IMessageHandlerContext>());

        handler.Verify(
            x => x.Handle(
                It.Is<VacancyApprovedEvent>(c => c.VacancyId == command.VacancyId)),
            Times.Once());
    }
}
