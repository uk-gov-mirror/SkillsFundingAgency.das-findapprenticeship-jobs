using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;

public interface IVacancyLiveHandler
{
    Task Handle(VacancyLiveEvent vacancyLiveEvent);
}