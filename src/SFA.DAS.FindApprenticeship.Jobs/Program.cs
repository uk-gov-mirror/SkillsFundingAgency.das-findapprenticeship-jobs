using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using SFA.DAS.Encoding;
using SFA.DAS.FindApprenticeship.Jobs.Application;
using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Application.Services;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Configuration;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Alerting;
using SFA.DAS.FindApprenticeship.Jobs.StartupExtensions;
using System.Security.Cryptography.X509Certificates;

[assembly: NServiceBusTriggerFunction("SFA.DAS.FindApprenticeship.Jobs")]
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder.BuildConfiguration())
    .ConfigureNServiceBus()
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(builder =>
            {
                builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
                builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);

                builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole();
            }
        );

        var configuration = context.Configuration;

        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configuration));
        services.AddOptions();

        services.Configure<FindApprenticeshipJobsConfiguration>(configuration.GetSection(nameof(FindApprenticeshipJobsConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FindApprenticeshipJobsConfiguration>>().Value);
        
        // Configure the DAS Encoding service
        var dasEncodingConfig = new EncodingConfig { Encodings = [] };
        context.Configuration.GetSection(nameof(dasEncodingConfig.Encodings)).Bind(dasEncodingConfig.Encodings);
        services.AddSingleton(dasEncodingConfig);
        services.AddSingleton<IEncodingService, EncodingService>();

        var environmentName = configuration["Values:EnvironmentName"] ?? configuration["EnvironmentName"];
        services.AddSingleton(new FunctionEnvironment(environmentName));

        var alertingConfiguration = new IndexingAlertingConfiguration();
        context.Configuration.GetSection(nameof(IndexingAlertingConfiguration)).Bind(alertingConfiguration);
        services.AddSingleton<IIndexingAlertingConfiguration>(alertingConfiguration);
        
        if (alertingConfiguration.Enabled)
        {
            services.AddHttpClient<ITeamsClient, TeamsClient>()
                .AddPolicyHandler(_ => HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        }
        else
        {
            services.AddSingleton<ITeamsClient, NoLoggingTeamsClient>();
        }
        
        services.AddSingleton<TokenCredential>(sp =>
            AzureCredentialFactory.BuildCredential(sp.GetRequiredService<IConfiguration>()));

        services.AddTransient<IApprenticeAzureSearchDocumentFactory, ApprenticeAzureSearchDocumentFactory>();
        services.AddTransient<IFindApprenticeshipJobsService, FindApprenticeshipJobsService>();
        services.AddTransient<IAzureSearchHelper, AzureSearchHelper>();
        services.AddTransient<IIndexingAlertsManager, IndexingAlertsManager>();
        services.AddTransient<IRecruitIndexerJobHandler, RecruitIndexerJobHandler>();
        services.AddTransient<IIndexCleanupJobHandler, IndexCleanupJobHandler>();
        services.AddTransient<IVacancyUpdatedHandler, VacancyUpdatedHandler>();
        services.AddTransient<IVacancyClosedHandler, VacancyClosedHandler>();
        services.AddTransient<IVacancyLiveHandler, VacancyLiveHandler>();
        services.AddTransient<IVacancyClosingSoonHandler, VacancyClosingSoonHandler>();
        services.AddTransient<ISendApplicationReminderHandler, SendApplicationReminderHandler>();
        services.AddTransient<IGetAllCandidatesWithSavedSearchesHandler, GetAllCandidatesWithSavedSearchesHandler>();
        services.AddTransient<IGetGetCandidateSavedSearchHandler, GetGetCandidateSavedSearchHandler>();
        services.AddTransient<IGetDormantCandidateAccountsHandler, GetDormantCandidateAccountsHandler>();
        services.AddTransient<ISendSavedSearchesNotificationHandler, SendSavedSearchesNotificationHandler>();
        services.AddTransient<IUpdateCandidateStatusHandler, UpdateCandidateStatusHandler>();
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IBatchTaskRunner, BatchTaskRunner>();
        services.AddHttpClient<IOuterApiClient, OuterApiClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<FindApprenticeshipJobsConfiguration>>().Value;

                var baseUrl = !string.IsNullOrEmpty(config.ApimBaseUrlSecure) && config.UseSecureGateway
                    ? config.ApimBaseUrlSecure
                    : config.ApimBaseUrl;

                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("ApimBaseUrl (or ApimBaseUrlSecure) is not configured.");

                client.BaseAddress = new Uri(baseUrl!);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var config = sp.GetRequiredService<IOptions<FindApprenticeshipJobsConfiguration>>().Value;
                var logger = sp.GetRequiredService<ILogger<FindApprenticeshipJobsConfiguration>>();

                if (string.IsNullOrEmpty(config.SecretClientUrl) || string.IsNullOrEmpty(config.SecretName))
                {
                    logger.LogInformation("No client cert configuration to add");
                    return new HttpClientHandler();
                }

                var secretClientOptions = new SecretClientOptions
                {
                    Retry =
                    {
                        NetworkTimeout = TimeSpan.FromSeconds(5),
                        MaxRetries = 3,
                        Mode = RetryMode.Exponential,
                        Delay = TimeSpan.FromMilliseconds(200),
                        MaxDelay = TimeSpan.FromSeconds(5)
                    }
                };

                try
                {
                    var credential = sp.GetRequiredService<TokenCredential>();
                    var secretClient = new SecretClient(new Uri(config.SecretClientUrl!), credential, secretClientOptions);

                    var secret = secretClient.GetSecret(config.SecretName!);
                    if (!secret.HasValue)
                    {
                        throw new Exception($"Has errored - {secret.GetRawResponse().Content.ToDynamicFromJson()}");
                    }

                    var handler = new HttpClientHandler();
                    handler.ClientCertificates.Add(
                        new X509Certificate2(Convert.FromBase64String(secret.Value.Value))
                    );

                    logger.LogInformation("Added client cert configuration");
                    return handler;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to add client cert configuration");

                    if (config.UseSecureGateway)
                        throw;

                    return new HttpClientHandler();
                }
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(10))
            .AddPolicyHandler(_ =>
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                        retryAttempt)));
            });
        
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();