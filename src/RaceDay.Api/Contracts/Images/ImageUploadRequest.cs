using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Contracts.Images;

// Multipart model binding and typed responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Exactly one JPEG or PNG file in a multipart form.</summary>
public sealed class ImageUploadRequest
{
    /// <summary>Required JPEG/PNG image, at most 5 MiB and 4096 pixels per side.</summary>
    [Required]
    public required IFormFile ImageFile { get; init; }
}

/// <summary>The saved profile picture's short-lived read URL.</summary>
public sealed record ProfilePictureResponse(string ProfilePictureUrl);

/// <summary>The saved event banner's short-lived read URL.</summary>
public sealed record EventBannerResponse(string BannerImageUrl);
