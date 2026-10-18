using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace RaceDay.Api.Authentication;

// Server-side caching and cookie protection: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes?view=net-10.0
// Accessed: 18 October 2026.
internal sealed record ServerSession(int UserId, string Role, DateTimeOffset ExpiresAt);

/// <summary>Stores login identity on the server and protects the browser's session cookie.</summary>
public sealed class SessionStore(
    IDistributedCache cache, IDataProtectionProvider protection, TimeProvider clock, IHostEnvironment environment)
{
    internal const string Scheme = "RaceDaySession";
    internal const string CookieName = "RaceDay.Session";
    internal const string Purpose = "RaceDay.Authentication.SessionCookie.v1";
    internal const string CachePrefix = "RaceDay:Session:";
    private readonly IDataProtector protector = protection.CreateProtector(Purpose);

    internal async Task<ServerSession> CreateAsync(HttpContext context, int userId, string role)
    {
        await RemoveAsync(context);
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var session = new ServerSession(userId, role, clock.GetUtcNow().AddMinutes(20));
        await cache.SetStringAsync(CachePrefix + key, JsonSerializer.Serialize(session),
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAt }, context.RequestAborted);
        context.Response.Cookies.Append(CookieName, protector.Protect(key), CookieOptions(context));
        return session;
    }

    internal async Task<ServerSession?> ReadAsync(HttpContext context)
    {
        string? key = ReadKey(context);
        if (key is null)
            return null;

        string? value = await cache.GetStringAsync(CachePrefix + key, context.RequestAborted);
        if (value is null)
            return null;

        var session = JsonSerializer.Deserialize<ServerSession>(value);
        if (session is null || session.ExpiresAt <= clock.GetUtcNow())
        {
            await RemoveAsync(context);
            return null;
        }
        return session;
    }

    internal async Task RemoveAsync(HttpContext context)
    {
        string? key = ReadKey(context);
        if (key is not null)
            await cache.RemoveAsync(CachePrefix + key, context.RequestAborted);

        if (context.Request.Cookies.ContainsKey(CookieName))
            context.Response.Cookies.Delete(CookieName, CookieOptions(context));
    }

    private string? ReadKey(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out string? value))
            return null;
        try
        {
            string key = protector.Unprotect(value);
            return key.Length == 64 && key.All(char.IsAsciiHexDigit) ? key : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = !environment.IsDevelopment() || context.Request.IsHttps,
        IsEssential = true,
        Path = "/"
    };
}
