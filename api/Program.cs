using DjPortalApi.Features.AiChat;
using DjPortalApi.Features.Contact;
using DjPortalApi.Features.Deployment;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Import;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Tracks;
using DjPortalApi.Features.WebSearch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddHttpClient()
    .AddMemoryCache();

builder.Services
    .AddScoped<IEmailService, EmailService>()
    .AddScoped<IDeploymentService, DeploymentService>()
    .AddScoped<IEventRepository, EventRepository>()
    .AddScoped<IEventService, EventService>()
    .AddScoped<IRequestRepository, RequestRepository>()
    .AddScoped<IRequestService, RequestService>()
    .AddScoped<ITrackRepository, TrackRepository>()
    .AddScoped<IWebSearchService, WebSearchService>()
    .AddScoped<IAiChatTools, AiChatTools>()
    .AddScoped<IAiChatService, AiChatService>()
    .AddScoped<ICerocClient, CerocClient>()
    .AddScoped<IEventImportService, EventImportService>();

// Ceroc pages are fetched on the DJ's behalf, so the client is kept on a short leash: a timeout, a size cap,
// and no redirects, so a link can never bounce the server off to another site.
builder.Services
    .AddHttpClient(CerocClient.HttpClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
        client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; EventImport/1.0)");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Build().Run();
