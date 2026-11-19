using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Allowed form fields, antiforgery and redirect-after-post: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Organiser")]
[Route("organiser/events")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OwnedEventsController(OrganiserApiClient api, ImageApiClient images, WebSessionStore sessions, ILogger<OwnedEventsController> logger) : Controller
{
    private const string Editable = "EventName,EventType,Description,DistanceKm,EventDateTime,EntryClosingDateTime,VenueName,AddressLine1,City,Province,Latitude,Longitude,BannerImage";

    [HttpGet("create")]
    public IActionResult Create() => View(new CreateEventForm());

    [HttpPost("create")]
    [RequestSizeLimit(ImageApiClient.FormLimit), RequestFormLimits(MultipartBodyLengthLimit = ImageApiClient.FormLimit)]
    public async Task<IActionResult> Create([Bind(Editable)] CreateEventForm model, CancellationToken cancellationToken)
    {
        CheckBanner(model.BannerImage);
        if (ModelState.IsValid)
        {
            try
            {
                var login = await Login();
                var saved = await api.CreateEvent(model, login, cancellationToken);
                TempData["EventSaved"] = "Your Draft event has been created.";
                return await BannerAfterSave(saved.EventId, model.BannerImage, login, cancellationToken);
            }
            catch (EventWriteException error) when (error.StatusCode == 400) { AddErrors(error); }
            catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
            { return await Failure(error, write: true); }
        }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View(model);
    }

    [HttpGet("{eventId:int:min(1)}")]
    public async Task<IActionResult> Details([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        try { return View(await api.OwnedEvent(eventId, await Login(), cancellationToken)); }
        catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpGet("{eventId:int:min(1)}/edit")]
    public async Task<IActionResult> Edit([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        try { return View(EditEventForm.From(await api.OwnedEvent(eventId, await Login(), cancellationToken))); }
        catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpPost("{eventId:int:min(1)}/edit")]
    [RequestSizeLimit(ImageApiClient.FormLimit), RequestFormLimits(MultipartBodyLengthLimit = ImageApiClient.FormLimit)]
    public async Task<IActionResult> Edit([FromRoute] int eventId, [Bind(Editable + ",Status")] EditEventForm model, CancellationToken cancellationToken)
    {
        bool submitted = false;
        try
        {
            var login = await Login();
            await api.OwnedEvent(eventId, login, cancellationToken);
            CheckBanner(model.BannerImage);
            if (ModelState.IsValid)
            {
                submitted = true;
                await api.UpdateEvent(eventId, model, login, cancellationToken);
                TempData["EventSaved"] = "Your event has been updated.";
                return await BannerAfterSave(eventId, model.BannerImage, login, cancellationToken);
            }
        }
        catch (EventWriteException error) when (error.StatusCode is 400 or 409) { AddErrors(error); }
        catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
        { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View(model);
    }

    [HttpGet("{eventId:int:min(1)}/delete")]
    public async Task<IActionResult> Delete([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        try { return View(await api.OwnedEvent(eventId, await Login(), cancellationToken)); }
        catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpPost("{eventId:int:min(1)}/delete")]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        EventDetails? saved = null;
        try
        {
            var login = await Login(); saved = await api.OwnedEvent(eventId, login, cancellationToken);
            await api.DeleteEvent(eventId, login, cancellationToken);
            TempData["EventSaved"] = "Your event has been deleted.";
            return RedirectToAction("Events", "Organiser");
        }
        catch (EventWriteException error) when (saved is not null && error.StatusCode == 409)
        {
            AddErrors(error); return View("Delete", saved);
        }
        catch (Exception error) when (error is ApiSessionEndedException or EventWriteException or ApiUnavailableException)
        { return await Failure(error, write: saved is not null); }
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();
    // A separate image write must never make the user repeat a successfully saved event.
    // Multipart requests: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
    // Accessed: 19 November 2026.
    private async Task<IActionResult> BannerAfterSave(int eventId, IFormFile? file, WebLogin login, CancellationToken cancellationToken)
    {
        if (file is null) return RedirectToAction(nameof(Details), new { eventId });
        try
        {
            await images.Upload(eventId, file, login, cancellationToken);
            TempData["ImageSaved"] = "The event banner has been saved.";
            return RedirectToAction(nameof(Details), new { eventId });
        }
        catch (ImageRequestException error) when (error.StatusCode is 400 or 413 or 415)
        { TempData["BannerNotice"] = "The event was saved, but its banner was rejected: " + error.Message; }
        catch (ApiUnavailableException)
        { TempData["BannerNotice"] = "The event was saved. The banner change could not be confirmed; check the current image before uploading again."; }
        catch (ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = $"/organiser/events/{eventId}/banner" });
        }
        catch (ImageRequestException error) when (error.StatusCode == 403) { return Forbid(); }
        catch (ImageRequestException error) when (error.StatusCode == 404) { return NotFound(); }
        return RedirectToAction("Banner", "Images", new { eventId });
    }

    private void CheckBanner(IFormFile? file)
    {
        if (file is null) return;
        try { ImageApiClient.CheckLength(file); }
        catch (ImageRequestException error) { ModelState.AddModelError("BannerImage", error.Message); Response.StatusCode = error.StatusCode; }
    }
    private async Task<IActionResult> Failure(Exception error, bool write = false)
    {
        if (error is ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
        }
        if (error is EventWriteException { StatusCode: 403 }) return Forbid();
        if (error is EventWriteException { StatusCode: 404 }) return NotFound();
        logger.LogWarning(error, "The owned-event request could not be completed.");
        ViewData["UnconfirmedEvent"] = write; Response.StatusCode = 503;
        return View("Unavailable");
    }

    private void AddErrors(EventWriteException error)
    {
        foreach (var entry in error.Errors)
        {
            string key = (Editable + ",Status").Split(',').FirstOrDefault(field => string.Equals(field, entry.Key, StringComparison.OrdinalIgnoreCase)) ?? "";
            foreach (string message in entry.Value ?? [])
                if (!string.IsNullOrWhiteSpace(message)) ModelState.AddModelError(key, message);
        }
        if (error.Errors.Count == 0) ModelState.AddModelError("", error.Message);
        Response.StatusCode = error.StatusCode;
    }
}
