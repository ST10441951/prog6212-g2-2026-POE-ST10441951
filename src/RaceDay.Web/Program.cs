using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;
using RaceDay.Web.Authentication;
using RaceDay.Web.Services;

// MVC, layouts and error pages: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
// Form protection: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
var builder = WebApplication.CreateBuilder(args);

// Server-side MVC sessions: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataProtection();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<WebSessionStore>();
// Authentication schemes and role checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
builder.Services.AddAuthentication(WebSessionHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, WebSessionHandler>(WebSessionHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection("Api"))
    .ValidateDataAnnotations()
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var address) &&
        address.Scheme is "http" or "https" && string.IsNullOrEmpty(address.UserInfo) &&
        string.IsNullOrEmpty(address.Query) && string.IsNullOrEmpty(address.Fragment) &&
        options.BaseUrl.EndsWith('/'),
        "Set Api:BaseUrl to an HTTP or HTTPS address ending in /, without credentials, a query or a fragment.")
    .ValidateOnStart();

// Typed clients and cookie isolation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// Accessed: 18 November 2026.
builder.Services.AddHttpClient<RaceDayApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<AccountApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<EntryApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<ProfileApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<ResultApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<OrganiserApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});

var app = builder.Build();

app.UseExceptionHandler("/error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

// Marker for the MVC test host, kept separate from the API's Program class.
namespace RaceDay.Web
{
    public partial class Program { }
}
