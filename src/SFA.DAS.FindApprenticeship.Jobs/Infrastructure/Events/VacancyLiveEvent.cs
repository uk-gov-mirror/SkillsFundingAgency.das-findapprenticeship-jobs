// ReSharper disable once CheckNamespace -- THIS MUST STAY LIKE THIS TO MATCH THE EVENT FROM RECRUIT
namespace SFA.DAS.Recruit.Api.Core.Events;

public sealed record VacancyLiveEvent(Guid VacancyId, long VacancyReference);