using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;

namespace SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;

public class SendApplicationReminderHandler(IFindApprenticeshipJobsService findApprenticeshipJobsService)
    : ISendApplicationReminderHandler
{
    public async Task Handle(long vacancyRef, int daysUntilExpiry)
    {
        await findApprenticeshipJobsService.SendApplicationClosingSoonReminder(vacancyRef, daysUntilExpiry);
    }
}