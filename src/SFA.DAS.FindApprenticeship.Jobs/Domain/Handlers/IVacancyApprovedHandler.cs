using Esfa.Recruit.Vacancies.Client.Domain.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
public interface IVacancyApprovedHandler
{
    Task Handle(VacancyApprovedEvent vacancyApprovedEvent);
}