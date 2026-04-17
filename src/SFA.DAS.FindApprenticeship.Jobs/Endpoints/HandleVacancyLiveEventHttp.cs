using System.Text.Json;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Endpoints;

public class HandleVacancyLiveEventHttp(IVacancyLiveHandler handler)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        
    [Function("HandleVacancyLiveEventHttp")]
    public async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage request)
    {
        var command = await JsonSerializer.DeserializeAsync<VacancyLiveEvent>(await request.Content.ReadAsStreamAsync(), JsonOptions);
        if (command == null || command.VacancyId == Guid.Empty)
        {
            throw new ArgumentException($"HandleVacancyLiveEvent HTTP trigger function found empty request at {DateTime.UtcNow}", nameof(request));
        }

        await handler.Handle(command);
    }
}