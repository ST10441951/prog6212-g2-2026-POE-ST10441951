using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Images;
using RaceDay.Api.Data;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

// Multipart files, role checks, SQL transactions and compensation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-delete
// Rules follow docs/API-Endpoint-Plan.md, sections 6 and 7.
// Accessed: 18 October 2026.
/// <summary>Own Participant pictures and owned-event banners.</summary>
[ApiController]
[Authorize]
[Route("api")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public sealed class ImagesController(RaceDayDbContext database, IImageStorage images, TimeProvider clock,
    ILogger<ImagesController> logger) : ControllerBase
{
    /// <summary>Upload or replace your own Participant profile picture.</summary>
    /// <remarks>
    /// Requires a Participant session. Send exactly one multipart imageFile, with no other form fields.
    /// Accepts JPEG (.jpg/.jpeg, image/jpeg) and PNG (.png, image/png), at most 5 MiB. The content,
    /// dimensions and decoding are checked; one still image up to 4096 pixels per side is permitted.
    /// The API re-encodes the image without metadata, generates its name and stores that name in SQL.
    /// Returns a read-only URL valid for one hour. GET /api/profile supplies a fresh URL later.
    /// The client cannot select another account or provide a blob name. Replacement removes the old image.
    /// </remarks>
    /// <response code="200">The profilePictureUrl of the saved image.</response>
    /// <response code="400">Missing, empty, damaged, animated, oversized dimensions or extra form fields.</response>
    /// <response code="413">The file or processed image exceeds 5 MiB, or the request is too large.</response>
    /// <response code="415">Use multipart form data and a matching JPEG/PNG name, MIME type and content.</response>
    [Authorize(Policy = "Participant")]
    [HttpPut("profile/picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageValidation.RequestLimit)]
    [ImageUploadLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageValidation.RequestLimit)]
    [ProducesResponseType<ProfilePictureResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public Task<IActionResult> Picture([FromForm] ImageUploadRequest request, CancellationToken cancellationToken) =>
        Change(null, request.ImageFile, false, cancellationToken);

    /// <summary>Remove your own Participant profile picture.</summary>
    /// <remarks>
    /// Requires a Participant session. No body or user ID is accepted. Removes the stored blob,
    /// clears its SQL name and updates the account timestamp. Repeating removal returns 204.
    /// Storage failures return 503 and leave the saved name available for a retry.
    /// </remarks>
    /// <response code="204">The picture was removed, or no picture was saved.</response>
    [Authorize(Policy = "Participant")]
    [HttpDelete("profile/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RemovePicture(CancellationToken cancellationToken) => Change(null, null, true, cancellationToken);

    /// <summary>Upload or replace a banner for your own event.</summary>
    /// <remarks>
    /// Requires the owning Organiser. Uses the same JPEG/PNG, multipart imageFile, 5 MiB, still-image,
    /// dimension and decoding checks as profile pictures. Event status does not prevent a banner correction.
    /// A generated name is stored in SQL; a read-only bannerImageUrl valid for one hour is returned.
    /// Event details and event lists supply fresh URLs. No account keys are returned.
    /// </remarks>
    /// <response code="200">The saved bannerImageUrl.</response>
    /// <response code="400">Missing or invalid image, or unexpected form fields.</response>
    /// <response code="413">The image, processed file or request exceeds its size limit.</response>
    /// <response code="415">The request or file format is not permitted.</response>
    [Authorize(Policy = "Organiser")]
    [HttpPut("events/{eventId:int}/banner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageValidation.RequestLimit)]
    [ImageUploadLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageValidation.RequestLimit)]
    [ProducesResponseType<EventBannerResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public Task<IActionResult> Banner(int eventId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken) =>
        Change(eventId, request.ImageFile, false, cancellationToken);

    /// <summary>Remove a banner from your own event.</summary>
    /// <remarks>
    /// Requires the event's Organiser. Takes the event ID and no body. Removes the blob and clears
    /// its SQL name. Repeating removal returns 204. A missing event returns 404; another owner gets 403.
    /// Storage failures return 503 and keep the saved name for a retry.
    /// </remarks>
    /// <response code="204">The banner was removed, or no banner was saved.</response>
    [Authorize(Policy = "Organiser")]
    [HttpDelete("events/{eventId:int}/banner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RemoveBanner(int eventId, CancellationToken cancellationToken) => Change(eventId, null, true, cancellationToken);

    private async Task<IActionResult> Change(int? eventId, IFormFile? file, bool remove, CancellationToken cancellationToken)
    {
        ValidatedImage? validated = null;
        if (!remove)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            if (form.Count != 0 || form.Files.Count != 1 || !string.Equals(form.Files[0].Name, "imageFile", StringComparison.OrdinalIgnoreCase))
                return Problem(statusCode: 400, title: "Send only one imageFile and no other form fields");
            try { validated = await ImageValidation.Read(file!, cancellationToken); }
            catch (InvalidImageException error) { return Problem(statusCode: error.Status, title: error.Message); }
        }

        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = eventId.HasValue ? await EventWriteLock.Query(database, eventId.Value).SingleOrDefaultAsync(cancellationToken) : null;
        var user = !eventId.HasValue ? await database.Users.FromSqlInterpolated(
            $"SELECT * FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = {userId}").SingleOrDefaultAsync(cancellationToken) : null;
        if (eventId.HasValue && item is null) return Problem(statusCode: 404, title: "Event not found");
        if (!eventId.HasValue && user is null) return Problem(statusCode: 404, title: "Account not found");
        if (item is not null && item.OrganiserUserId != userId) return Problem(statusCode: 403, title: "You do not manage this event");
        string? oldName = item is not null ? item.BannerImageBlobName : user!.ProfilePictureBlobName;
        if (remove && oldName is null) return NoContent();
        string? newName = remove ? null : $"{(item is null ? $"profile-pictures/user-{userId}" : $"event-banners/event-{eventId}")}/{Guid.NewGuid():N}.{validated!.Extension}";
        bool uploaded = false;
        try
        {
            if (remove) await images.Delete(oldName!, cancellationToken);
            else
            {
                await using var content = new MemoryStream(validated!.Content, false);
                await images.Upload(newName!, content, validated.ContentType, cancellationToken);
                uploaded = true;
            }
            if (item is not null) { item.BannerImageBlobName = newName; item.UpdatedAt = clock.GetUtcNow().UtcDateTime; }
            else { user!.ProfilePictureBlobName = newName; user.UpdatedAt = clock.GetUtcNow().UtcDateTime; }
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (ImageStorageException)
        {
            if (!remove) await Cleanup(newName!);
            logger.LogError("Image storage failed for {Resource} {Id}.", eventId.HasValue ? "event" : "user", eventId ?? userId);
            return Problem(statusCode: 503, title: "Image storage is unavailable; try again later");
        }
        catch
        {
            if (uploaded) await Cleanup(newName!);
            throw;
        }
        if (remove) return NoContent();
        if (oldName is not null) await Cleanup(oldName);
        string url = images.ReadUrl(newName) ?? throw new ImageStorageException("A saved image needs a readable URL.");
        return item is null ? Ok(new ProfilePictureResponse(url)) : Ok(new EventBannerResponse(url));
    }

    private async Task Cleanup(string blobName)
    {
        try { await images.Delete(blobName, CancellationToken.None); }
        catch (ImageStorageException) { logger.LogWarning("Image cleanup needs a retry for {BlobName}.", blobName); }
    }
}
