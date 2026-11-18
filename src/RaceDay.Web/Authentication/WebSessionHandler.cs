using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RaceDay.Web.Services;

namespace RaceDay.Web.Authentication;

// Session authentication, role claims and local redirects: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-10.0
// Accessed: 18 November 2026.
internal sealed class WebSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, WebSessionStore sessions, AccountApiClient api, TimeProvider clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "WebSession";
    internal const string UnavailableKey = "RaceDay.AccountUnavailable";
    private bool unavailable;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var login = await sessions.ReadAsync(Context);
        if (login is null) return AuthenticateResult.NoResult();
        SessionAccount account;
        try { account = await api.GetSessionAccountAsync(login.ApiCookie, Context.RequestAborted); }
        catch (ApiSessionEndedException)
        {
            await sessions.RemoveAsync(Context);
            return AuthenticateResult.NoResult();
        }
        catch (ApiUnavailableException)
        {
            unavailable = true;
            Context.Items[UnavailableKey] = true;
            Logger.LogWarning("The API session could not be checked; protected access was refused.");
            return AuthenticateResult.Fail("Account service unavailable.");
        }

        if (!account.IsActive || account.UserId != login.UserId || account.Role != login.Role ||
            login.ExpiresAt <= clock.GetUtcNow())
        {
            await sessions.RemoveAsync(Context);
            return AuthenticateResult.NoResult();
        }
        // A concurrent sign-out must not be undone by a slow profile response.
        if (await sessions.ReadAsync(Context) is null) return AuthenticateResult.NoResult();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, login.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, account.FirstName),
            new Claim(ClaimTypes.Role, login.Role)
        ], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.CacheControl = "no-store";
        if (unavailable)
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        else
        {
            string address = "/account/login";
            if (HttpMethods.IsGet(Request.Method))
                address = QueryHelpers.AddQueryString(address, "returnUrl", Request.PathBase + Request.Path + Request.QueryString);
            Response.Redirect(Request.PathBase + address);
        }
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Headers.CacheControl = "no-store";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
