using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Images;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Protected uploads, confirmation pages and explicit form binding: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 19 November 2026.
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ImagesController(ImageApiClient images, ProfileApiClient profiles, OrganiserApiClient events, WebSessionStore sessions, ILogger<ImagesController> logger) : Controller
{
    [Authorize(Roles = "Participant"), HttpGet("profile/picture")]
    public Task<IActionResult> Picture(CancellationToken cancellationToken) => Open(null, false, cancellationToken);
    [Authorize(Roles = "Organiser"), HttpGet("organiser/events/{eventId:int:min(1)}/banner")]
    public Task<IActionResult> Banner([FromRoute] int eventId, CancellationToken cancellationToken) => Open(eventId, false, cancellationToken);

    [Authorize(Roles = "Participant"), HttpPost("profile/picture")]
    [RequestSizeLimit(ImageApiClient.FormLimit), RequestFormLimits(MultipartBodyLengthLimit = ImageApiClient.FormLimit)]
    public Task<IActionResult> Picture([Bind("ImageFile")] ImageUploadForm form, CancellationToken cancellationToken) => Save(null, form, cancellationToken);
    [Authorize(Roles = "Organiser"), HttpPost("organiser/events/{eventId:int:min(1)}/banner")]
    [RequestSizeLimit(ImageApiClient.FormLimit), RequestFormLimits(MultipartBodyLengthLimit = ImageApiClient.FormLimit)]
    public Task<IActionResult> Banner([FromRoute] int eventId, [Bind("ImageFile")] ImageUploadForm form, CancellationToken cancellationToken) => Save(eventId, form, cancellationToken);

    [Authorize(Roles = "Participant"), HttpGet("profile/picture/remove")]
    public Task<IActionResult> RemovePicture(CancellationToken cancellationToken) => Open(null, true, cancellationToken);
    [Authorize(Roles = "Organiser"), HttpGet("organiser/events/{eventId:int:min(1)}/banner/remove")]
    public Task<IActionResult> RemoveBanner([FromRoute] int eventId, CancellationToken cancellationToken) => Open(eventId, true, cancellationToken);
    [Authorize(Roles = "Participant"), HttpPost("profile/picture/remove")]
    public Task<IActionResult> RemovePictureConfirmed(CancellationToken cancellationToken) => Remove(null, cancellationToken);
    [Authorize(Roles = "Organiser"), HttpPost("organiser/events/{eventId:int:min(1)}/banner/remove")]
    public Task<IActionResult> RemoveBannerConfirmed([FromRoute] int eventId, CancellationToken cancellationToken) => Remove(eventId, cancellationToken);

    private async Task<IActionResult> Open(int? eventId, bool remove, CancellationToken cancellationToken)
    {
        try
        {
            var page = await Context(eventId, await Login(), cancellationToken);
            return remove ? View("Remove", page) : View("Upload", new ImageUploadForm { Page = page });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    private async Task<IActionResult> Save(int? eventId, ImageUploadForm form, CancellationToken cancellationToken)
    {
        bool submitted = false;
        try
        {
            var login = await Login(); form.Page = await Context(eventId, login, cancellationToken);
            if (!Request.HasFormContentType || Request.Form.Files.Count != 1 || !string.Equals(Request.Form.Files[0].Name, "ImageFile", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("ImageFile", "Choose exactly one image file.");
            if (ModelState.IsValid)
            {
                submitted = true; await images.Upload(eventId, form.ImageFile!, login, cancellationToken);
                TempData["ImageSaved"] = "The image has been saved.";
                return Back(eventId);
            }
        }
        catch (ImageRequestException error) when (form.Page is not null && error.StatusCode is 400 or 413 or 415)
        { ModelState.AddModelError("ImageFile", error.Message); Response.StatusCode = error.StatusCode; }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        form.ImageFile = null;
        return View("Upload", form);
    }

    private async Task<IActionResult> Remove(int? eventId, CancellationToken cancellationToken)
    {
        bool submitted = false;
        try
        {
            var login = await Login(); await Context(eventId, login, cancellationToken);
            submitted = true; await images.Remove(eventId, login, cancellationToken);
            TempData["ImageSaved"] = "The image has been removed.";
            return Back(eventId);
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
    }

    private async Task<ImagePage> Context(int? eventId, WebLogin login, CancellationToken cancellationToken)
    {
        if (eventId is { } id)
        {
            var item = await events.OwnedEvent(id, login, cancellationToken);
            return new ImagePage { EventId = id, Name = item.EventName, ImageUrl = WebLinks.HttpUrl(item.BannerImageUrl) };
        }
        var profile = await profiles.Get(login, cancellationToken);
        return new ImagePage { Name = profile.FirstName + " " + profile.LastName, ImageUrl = WebLinks.HttpUrl(profile.ProfilePictureUrl) };
    }
    private IActionResult Back(int? eventId) => eventId is { } id ? RedirectToAction("Details", "OwnedEvents", new { eventId = id }) : RedirectToAction("Index", "Profile");
    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();
    private static bool Expected(Exception error) => error is ApiSessionEndedException or EventWriteException or ImageRequestException or ApiUnavailableException;
    private async Task<IActionResult> Failure(Exception error, bool write = false)
    {
        if (error is ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
        }
        if (error is EventWriteException { StatusCode: 403 } or ImageRequestException { StatusCode: 403 }) return Forbid();
        if (error is EventWriteException { StatusCode: 404 } or ImageRequestException { StatusCode: 404 }) return NotFound();
        logger.LogWarning("The image request could not be confirmed ({ErrorType}).", error.GetType().Name);
        ViewData["UnconfirmedImage"] = write; Response.StatusCode = 503;
        return View("Unavailable");
    }
}
