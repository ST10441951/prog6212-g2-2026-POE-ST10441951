using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;
using Microsoft.AspNetCore.Authorization;

namespace RaceDay.Api.Authentication;

// Authentication handlers and role claims: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 October 2026.
internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, SessionStore sessions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private bool inactiveAccount;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null &&
            endpoint.Metadata.GetMetadata<OptionalSessionAttribute>() is null)
            return AuthenticateResult.NoResult();

        var session = await sessions.ReadAsync(Context);
        if (session is null)
            return AuthenticateResult.NoResult();

        // Resolve SQL only after a session is found, so public host checks need no database.
        var database = Context.RequestServices.GetRequiredService<RaceDayDbContext>();
        var user = await database.Users.AsNoTracking().Where(x => x.UserId == session.UserId)
            .Select(x => new { x.IsActive, Role = x.Role.RoleName })
            .SingleOrDefaultAsync(Context.RequestAborted);

        if (user is null || !user.IsActive || user.Role != session.Role ||
            session.Role is not ("Organiser" or "Participant"))
        {
            inactiveAccount = user is { IsActive: false };
            await sessions.RemoveAsync(Context);
            return AuthenticateResult.Fail("The account no longer has a valid session.");
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, session.Role)
        ], SessionStore.Scheme);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Results.Problem(statusCode: inactiveAccount ? 403 : 401,
            title: inactiveAccount ? "Account inactive" : "Authentication required").ExecuteAsync(Context);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(statusCode: 403, title: "Access denied").ExecuteAsync(Context);
}
