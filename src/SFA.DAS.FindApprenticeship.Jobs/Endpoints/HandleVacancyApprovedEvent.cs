using Esfa.Recruit.Vacancies.Client.Domain.Events;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Endpoints;

public class HandleVacancyApprovedEvent(IVacancyLiveHandler vacancyLiveHandler, ILogger<HandleVacancyApprovedEvent> log) : IHandleMessages<VacancyApprovedEvent>
{
    public async Task Handle(VacancyApprovedEvent vacancyApprovedEvent, IMessageHandlerContext context)
    {
        log.LogInformation("NServiceBus VacancyApproved trigger function executed at {DateTime}", DateTime.UtcNow);
        await vacancyLiveHandler.Handle(new VacancyLiveEvent(vacancyApprovedEvent.VacancyId, vacancyApprovedEvent.VacancyReference));
        log.LogInformation("NServiceBus VacancyApproved trigger function finished at {DateTime}", DateTime.UtcNow);
    }
}