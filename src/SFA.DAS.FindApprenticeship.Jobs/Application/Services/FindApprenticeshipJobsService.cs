using SFA.DAS.Common.Domain.Models;
using SFA.DAS.FindApprenticeship.Jobs.Domain;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Candidate;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Domain.SavedSearches;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Alerting;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Requests;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.Application.Services;
public class FindApprenticeshipJobsService(IOuterApiClient apiClient, IIndexingAlertsManager indexingAlertsManager) : IFindApprenticeshipJobsService
{
    public async Task<GetLiveVacanciesApiResponse> GetLiveVacancies(int pageNumber, int pageSize, DateTime? closingDate = null)
    {
        var liveVacancies = await apiClient.Get<GetLiveVacanciesApiResponse>(new GetLiveVacanciesApiRequest(pageNumber, pageSize, closingDate));
        if (liveVacancies.StatusCode == HttpStatusCode.OK)
        {
            return liveVacancies.Body;    
        }

        await indexingAlertsManager.SendFaaImportAlertAsync();
        throw new HttpRequestException($"Failed to get live vacancies: {liveVacancies.StatusCode} ex: {liveVacancies.ErrorContent}");
    }

    public async Task<GetLiveVacancyApiResponse> GetLiveVacancy(VacancyReference vacancyReference)
    {
        var liveVacancy = await apiClient.Get<GetLiveVacancyApiResponse>(new GetLiveVacancyApiRequest(vacancyReference.ToShortString()));
        return liveVacancy.Body;
    }

    public async Task<GetNhsLiveVacanciesApiResponse?> GetNhsLiveVacancies()
    {
        var liveVacancies = await apiClient.Get<GetNhsLiveVacanciesApiResponse>(new GetNhsLiveVacanciesApiRequest());
        return liveVacancies.Body;
    }

    public async Task<GetCivilServiceLiveVacanciesApiResponse?> GetCivilServiceLiveVacancies()
    {
        var liveVacancies = await apiClient.Get<GetCivilServiceLiveVacanciesApiResponse>(new GetCivilServiceVacanciesApiRequest());
        return liveVacancies.Body;
    }

    public async Task SendApplicationClosingSoonReminder(VacancyReference vacancyReference, int daysUntilExpiry)
    {
        await apiClient.Post<NullResponse>(new PostSendApplicationClosingSoonRequest(vacancyReference.Value, daysUntilExpiry));
    }

    public async Task CloseVacancyEarly(VacancyReference vacancyRef)
    {
        await apiClient.Post<NullResponse>(new PostVacancyClosedEarlyRequest(vacancyRef.Value));
    }

    public async Task<GetCandidateSavedSearchesApiResponse> GetSavedSearches(int pageNumber,
        int pageSize,
        string lastRunDateTime,
        int maxApprenticeshipSearchResultCount = 5,
        string sortOrder = "AgeDesc")
    {
        var savedSearches = await apiClient.Get<GetCandidateSavedSearchesApiResponse>(new GetSavedSearchesApiRequest(
            pageNumber,
            pageSize,
            lastRunDateTime,
            maxApprenticeshipSearchResultCount));
        return savedSearches.Body;
    }

    public async Task SendSavedSearchNotification(SavedSearchCandidateVacancies savedSearchCandidateVacancies)
    {
        await apiClient.PostWithResponseCode<NullResponse>(new PostSendSavedSearchNotificationApiRequest(savedSearchCandidateVacancies));
    }

    public async Task<SavedSearchCandidateVacancies?> GetSavedSearchResultsForCandidate(SavedSearchResult request)
    {
        var actual = await apiClient.PostWithResponseCode<SavedSearchCandidateVacancies>(new PostGetSavedSearchResultsForCandidateRequest(request));
        
        return actual.StatusCode == HttpStatusCode.NotFound
            ? null
            : actual.Body;
    }

    public async Task<GetInactiveCandidatesApiResponse> GetDormantCandidates(string cutOffDateTime, int pageNumber, int pageSize)
    {
        var candidates = await apiClient.Get<GetInactiveCandidatesApiResponse>(new GetInactiveCandidatesApiRequest(cutOffDateTime, pageNumber, pageSize));
        return candidates.Body;
    }

    public async Task UpdateCandidateStatus(string govIdentifier, string email, CandidateStatus status)
    {
        await apiClient.PostWithResponseCode<NullResponse>(new PostUpdateCandidateStatusApiRequest(govIdentifier, new CandidateUpdateStatusRequest()
        {
            Email = email,
            Status = status
        }));
    }
}
