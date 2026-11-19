using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Services;

// Streamed multipart requests, isolated cookies and safe responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.net.http.multipartformdatacontent
// Contract: docs/API-Endpoint-Plan.md, sections 6 and 7.
// Accessed: 19 November 2026.
public sealed class ImageApiClient(HttpClient client)
{
    public const long FileLimit = 5 * 1024 * 1024;
    public const long FormLimit = FileLimit + 64 * 1024;

    internal async Task Upload(int? eventId, IFormFile file, WebLogin login, CancellationToken cancellationToken)
    {
        CheckLength(file);
        string name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name)) throw new ImageRequestException(400, "Choose a named JPEG or PNG image.");
        using var request = new HttpRequestMessage(HttpMethod.Put, Route(eventId));
        using var multipart = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var type) ? type : new MediaTypeHeaderValue("application/octet-stream");
        multipart.Add(content, "imageFile", name); request.Content = multipart;
        using var response = await Send(request, HttpStatusCode.OK, login, cancellationToken);
        ImageReply? reply;
        try { reply = await response.Content.ReadFromJsonAsync<ImageReply>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The saved image response was unreadable.", error); }
        if (WebLinks.HttpUrl(eventId is null ? reply?.ProfilePictureUrl : reply?.BannerImageUrl) is null)
            throw new ApiUnavailableException("The API did not confirm a readable saved image.");
    }

    internal async Task Remove(int? eventId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, Route(eventId));
        using var response = await Send(request, HttpStatusCode.NoContent, login, cancellationToken);
    }

    internal static void CheckLength(IFormFile file)
    {
        if (file.Length == 0) throw new ImageRequestException(400, "Choose a non-empty image.");
        if (file.Length > FileLimit) throw new ImageRequestException(413, "Choose an image no larger than 5 MiB.");
    }

    private static string Route(int? eventId) => eventId is { } id ? $"api/events/{id.ToString(CultureInfo.InvariantCulture)}/banner" : "api/profile/picture";
    private async Task<HttpResponseMessage> Send(HttpRequestMessage request, HttpStatusCode expected, WebLogin login, CancellationToken cancellationToken)
    {
        request.Headers.Add("Cookie", login.ApiCookie);
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The image request could not be confirmed.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The image request did not respond in time.", error); }
        if (response.StatusCode == expected) return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ApiSessionEndedException();
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnsupportedMediaType)
            {
                ImageReply? problem = null;
                try { problem = await response.Content.ReadFromJsonAsync<ImageReply>(cancellationToken); } catch (JsonException) { }
                throw new ImageRequestException((int)response.StatusCode, problem?.Title is { Length: > 0 and <= 300 } title ? title : "The image was rejected. Choose a valid JPEG or PNG and try again.");
            }
            throw new ApiUnavailableException("The images API returned an unexpected response.");
        }
    }
    private sealed class ImageReply
    {
        public string? ProfilePictureUrl { get; init; }
        public string? BannerImageUrl { get; init; }
        public string? Title { get; init; }
    }
}

internal sealed class ImageRequestException(int statusCode, string message) : Exception(message)
{
    internal int StatusCode { get; } = statusCode;
}
