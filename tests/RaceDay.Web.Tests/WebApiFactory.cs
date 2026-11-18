using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

// WebApplicationFactory and test-only services: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Accessed: 18 November 2026.
internal sealed class WebApiFactory : WebApplicationFactory<RaceDay.Web.Program>
{
    internal const string EventJson = """
        {"page":1,"pageSize":3,"totalCount":1,"totalPages":1,"items":[
          {"eventId":2,"eventName":"Cape Peninsula Cycle Challenge","eventType":"Cycling",
           "distanceKm":80,"eventDateTime":"2027-03-14T04:00:00Z",
           "city":"Cape Town","province":"Western Cape","status":"Open"}
        ]}
        """;

    internal Dictionary<string, string?> Settings { get; } = new()
    {
        ["Api:BaseUrl"] = "https://api.example.test/",
        ["Api:TimeoutSeconds"] = "15"
    };
    internal Func<HttpRequestMessage, HttpResponseMessage>? Reply { get; set; } = _ => Json(EventJson);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, settings) => settings.AddInMemoryCollection(Settings));
        builder.ConfigureServices(services =>
        {
            services.AddControllersWithViews().AddApplicationPart(typeof(FailureController).Assembly);
            if (Reply is not null)
                services.AddHttpClient<RaceDayApiClient>().ConfigurePrimaryHttpMessageHandler(() => new ApiHandler(Reply));
        });
    }

    internal HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    internal static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class ApiHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(reply(request));
    }
}
