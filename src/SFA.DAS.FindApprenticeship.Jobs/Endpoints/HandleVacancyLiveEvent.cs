using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Endpoints;

public class HandleVacancyLiveEvent(IVacancyLiveHandler handler) : IHandleMessages<VacancyLiveEvent>
{
    public async Task Handle(VacancyLiveEvent vacancyLiveEvent, IMessageHandlerContext context)
    {
        await handler.Handle(vacancyLiveEvent);
    }
}