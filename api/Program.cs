using DjPortalApi.Features.Events;
using DjPortalApi.Features.Insights;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Spotify;
using DjPortalApi.Features.Tracks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddScoped<IEventRepository, EventRepository>()
    .AddScoped<IEventService, EventService>()
    .AddScoped<IInsightsService, InsightsService>()
    .AddScoped<IRequestRepository, RequestRepository>()
    .AddScoped<ISpotifyService, SpotifyService>()
    .AddScoped<ITrackRepository, TrackRepository>();

builder.Build().Run();
