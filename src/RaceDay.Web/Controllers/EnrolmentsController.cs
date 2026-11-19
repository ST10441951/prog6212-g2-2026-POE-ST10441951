using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Role/ownership checks, form binding and antiforgery: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int:min(1)}/enrolments")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EnrolmentsController(OrganiserApiClient api, WebSessionStore sessions, ILogger<EnrolmentsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromRoute] int eventId, [FromQuery] RosterSearch search, CancellationToken cancellationToken)
    {
        search.Status = string.IsNullOrWhiteSpace(search.Status) ? null : search.Status.Trim();
        try
        {
            var login = await Login(); var item = await api.OwnedEvent(eventId, login, cancellationToken);
            if (ModelState.IsValid)
            {
                try { return View(new RosterListPage { Event = item, Search = search, Entries = await api.EventRoster(eventId, search, login, cancellationToken) }); }
                catch (EventWriteException error) when (error.StatusCode == 400) { }
            }
            Response.StatusCode = 400;
            return View(new RosterListPage { Event = item, Search = search, Error = "Choose a valid entry status, event category and page number." });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}")]
    public async Task<IActionResult> Details([FromRoute] int eventId, [FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Login(); await api.OwnedEvent(eventId, login, cancellationToken);
            return View(await api.RosterDetails(eventId, enrolmentId, login, cancellationToken));
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}/bib")]
    public async Task<IActionResult> Bib([FromRoute] int eventId, [FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Login(); await api.OwnedEvent(eventId, login, cancellationToken);
            var entry = await api.RosterDetails(eventId, enrolmentId, login, cancellationToken);
            return View(new BibNumberForm { BibNumber = entry.BibNumber ?? "", Entry = entry });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpPost("{enrolmentId:int:min(1)}/bib")]
    public async Task<IActionResult> Bib([FromRoute] int eventId, [FromRoute] int enrolmentId, [Bind("BibNumber")] BibNumberForm model, CancellationToken cancellationToken)
    {
        bool submitted = false;
        try
        {
            var login = await Login(); await api.OwnedEvent(eventId, login, cancellationToken);
            model.Entry = await api.RosterDetails(eventId, enrolmentId, login, cancellationToken);
            if (ModelState.IsValid)
            {
                submitted = true; await api.SaveBibNumber(model.Entry, model, login, cancellationToken);
                TempData["BibSaved"] = true;
                return RedirectToAction(nameof(Details), new { eventId, enrolmentId });
            }
        }
        catch (EventWriteException error) when (model.Entry is not null && error.StatusCode is 400 or 409)
        {
            bool added = false;
            foreach (var entry in error.Errors)
                foreach (string message in entry.Value ?? [])
                    if (!string.IsNullOrWhiteSpace(message))
                    { ModelState.AddModelError(string.Equals(entry.Key, "BibNumber", StringComparison.OrdinalIgnoreCase) ? "BibNumber" : "", message); added = true; }
            if (!added) ModelState.AddModelError("", error.Message);
            Response.StatusCode = error.StatusCode;
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View(model);
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();
    private static bool Expected(Exception error) => error is ApiSessionEndedException or EventWriteException or ApiUnavailableException;
    private async Task<IActionResult> Failure(Exception error, bool write = false)
    {
        if (error is ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
        }
        if (error is EventWriteException { StatusCode: 403 }) return Forbid();
        if (error is EventWriteException { StatusCode: 404 }) return NotFound();
        logger.LogWarning(error, "The event-enrolment request could not be completed.");
        ViewData["UnconfirmedBib"] = write; Response.StatusCode = 503;
        return View("Unavailable");
    }
}
