using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using System.Text.Json;
using Esfa.Recruit.Vacancies.Client.Domain.Events;
using SFA.DAS.Recruit.Api.Core.Events;

namespace SFA.DAS.FindApprenticeship.Jobs.Endpoints
{
    public class HandleVacancyApprovedEventHttp(IVacancyLiveHandler handler, ILogger<HandleVacancyApprovedEventHttp> log)
    {
        [Function("HandleVacancyApprovedEventHttp")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req)
        {
            log.LogInformation("HandleVacancyApprovedEvent HTTP trigger function executed at {DateTime}", DateTime.UtcNow);

            var command = await JsonSerializer.DeserializeAsync<VacancyApprovedEvent>(
                await req.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (command == null || command.VacancyId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"HandleVacancyApprovedEvent HTTP trigger function found empty request at {DateTime.UtcNow}",
                    nameof(req));
            }

            await handler.Handle(new VacancyLiveEvent(command.VacancyId, command.VacancyReference));
            log.LogInformation("HandleVacancyApprovedEvent HTTP trigger function finished at {DateTime}", DateTime.UtcNow);
        }
    }
}
