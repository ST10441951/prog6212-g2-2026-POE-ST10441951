namespace RaceDay.Web.Services;

// Allow only absolute web addresses in API-supplied links and image sources.
// Microsoft (2026): https://learn.microsoft.com/en-us/dotnet/api/system.uri.trycreate
// https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public static class WebLinks
{
    public static string? HttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var address) && address.Scheme is "http" or "https" &&
        string.IsNullOrEmpty(address.UserInfo) ? address.AbsoluteUri : null;
}
