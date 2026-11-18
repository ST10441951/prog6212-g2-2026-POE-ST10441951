using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace RaceDay.Web.Services;

// Server-side state, protected cookies and random identifiers: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes?view=net-10.0
// Accessed: 18 November 2026.
internal sealed record WebLogin(int UserId, string FirstName, string Role, DateTimeOffset ExpiresAt, string ApiCookie);

public sealed class WebSessionStore(IDistributedCache cache, IDataProtectionProvider protection,
    TimeProvider clock, IHostEnvironment environment)
{
    internal const string CookieName = "RaceDay.Web.Session";
    private const string CachePrefix = "RaceDay:WebSession:";
    private readonly IDataProtector protector = protection.CreateProtector("RaceDay.Web.Session.v1");

    internal async Task CreateAsync(HttpContext context, WebLogin login)
    {
        await RemoveAsync(context);
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await cache.SetStringAsync(CachePrefix + key, JsonSerializer.Serialize(login),
            new DistributedCacheEntryOptions { AbsoluteExpiration = login.ExpiresAt }, context.RequestAborted);
        context.Response.Cookies.Append(CookieName, protector.Protect(key), Options(context));
    }

    internal async Task<WebLogin?> ReadAsync(HttpContext context)
    {
        string? key = ReadKey(context);
        if (key is null) return null;
        string? value = await cache.GetStringAsync(CachePrefix + key, context.RequestAborted);
        if (value is null) return null;
        WebLogin? login;
        try { login = JsonSerializer.Deserialize<WebLogin>(value); }
        catch (JsonException) { login = null; }
        if (login is null || login.ExpiresAt <= clock.GetUtcNow() || login.UserId <= 0 ||
            login.Role is not ("Organiser" or "Participant"))
        {
            await RemoveAsync(context);
            return null;
        }
        return login;
    }

    internal async Task RemoveAsync(HttpContext context)
    {
        string? key = ReadKey(context);
        if (key is not null) await cache.RemoveAsync(CachePrefix + key, context.RequestAborted);
        if (context.Request.Cookies.ContainsKey(CookieName))
            context.Response.Cookies.Delete(CookieName, Options(context));
    }

    private string? ReadKey(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out string? value)) return null;
        try
        {
            string key = protector.Unprotect(value);
            return key.Length == 64 && key.All(char.IsAsciiHexDigit) ? key : null;
        }
        catch (Exception error) when (error is CryptographicException or FormatException) { return null; }
    }

    private CookieOptions Options(HttpContext context) => new()
    {
        HttpOnly = true, SameSite = SameSiteMode.Strict,
        Secure = !environment.IsDevelopment() || context.Request.IsHttps,
        IsEssential = true, Path = "/"
    };
}
