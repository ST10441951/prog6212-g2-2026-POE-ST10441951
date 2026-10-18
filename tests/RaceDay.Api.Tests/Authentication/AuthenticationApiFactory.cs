using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RaceDay.Api.Tests.Authentication;

// Test-only endpoints exercise the real authentication and authorization pipeline.
// Microsoft (2026): https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Accessed: 18 October 2026.
internal sealed class AuthenticationApiFactory(string connectionString, string environment = "Development")
    : WebApplicationFactory<Program>
{
    internal TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:RaceDay"] = connectionString }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddControllers().AddApplicationPart(typeof(AccessCheckController).Assembly);
        });
    }

    internal HttpClient Client(bool cookies = true, string scheme = "https") =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"{scheme}://localhost"),
            HandleCookies = cookies,
            AllowAutoRedirect = false
        });
}

internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    internal void Advance(TimeSpan duration) => now += duration;
}

// Added only by AuthenticationApiFactory. These routes are never added to the API project.
[ApiController]
[Route("__tests/access")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AccessCheckController : ControllerBase
{
    [HttpGet("signed-in")]
    public IActionResult SignedIn() => Ok(new
    {
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        Role = User.FindFirstValue(ClaimTypes.Role)
    });

    [Authorize(Policy = "Organiser")]
    [HttpGet("organiser")]
    public IActionResult Organiser() => Ok();

    [Authorize(Policy = "Participant")]
    [HttpGet("participant")]
    public IActionResult Participant() => Ok();
}
