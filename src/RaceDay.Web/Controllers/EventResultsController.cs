using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Role restrictions, explicit binding and protected forms: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 19 November 2026.
[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int:min(1)}/results")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventResultsController(OrganiserApiClient api, WebSessionStore sessions, ILogger<EventResultsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromRoute] int eventId, [FromQuery] EventResultSearch search, CancellationToken cancellationToken)
    {
        search.ResultStatus = string.IsNullOrWhiteSpace(search.ResultStatus) ? null : search.ResultStatus.Trim();
        try
        {
            var login = await Login(); var item = await api.OwnedEvent(eventId, login, cancellationToken);
            if (ModelState.IsValid)
            {
                try { return View(new EventResultList { Event = item, Search = search, Results = await api.EventResults(eventId, search, login, cancellationToken) }); }
                catch (EventWriteException error) when (error.StatusCode == 400) { }
            }
            Response.StatusCode = 400;
            return View(new EventResultList { Event = item, Search = search, Error = "Choose a valid result status, event category and page number." });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}")]
    public async Task<IActionResult> Details([FromRoute] int eventId, [FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Login(); var entry = await Entry(eventId, enrolmentId, login, cancellationToken);
            return View(await api.OfficialResult(entry, login, cancellationToken));
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}/capture")]
    public Task<IActionResult> Capture([FromRoute] int eventId, [FromRoute] int enrolmentId, CancellationToken cancellationToken) => OpenForm(eventId, enrolmentId, false, cancellationToken);

    [HttpGet("{enrolmentId:int:min(1)}/edit")]
    public Task<IActionResult> Edit([FromRoute] int eventId, [FromRoute] int enrolmentId, CancellationToken cancellationToken) => OpenForm(eventId, enrolmentId, true, cancellationToken);

    [HttpPost("{enrolmentId:int:min(1)}/capture")]
    public Task<IActionResult> Capture([FromRoute] int eventId, [FromRoute] int enrolmentId,
        [Bind("ResultStatus,FinishTime,OverallPosition,CategoryPosition,Notes")] ResultForm form, CancellationToken cancellationToken) => Save(eventId, enrolmentId, form, false, cancellationToken);

    [HttpPost("{enrolmentId:int:min(1)}/edit")]
    public Task<IActionResult> Edit([FromRoute] int eventId, [FromRoute] int enrolmentId,
        [Bind("ResultStatus,FinishTime,OverallPosition,CategoryPosition,Notes")] ResultForm form, CancellationToken cancellationToken) => Save(eventId, enrolmentId, form, true, cancellationToken);

    private async Task<IActionResult> OpenForm(int eventId, int enrolmentId, bool edit, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Login(); var entry = await Entry(eventId, enrolmentId, login, cancellationToken);
            var form = new ResultForm { Entry = entry };
            if (edit)
            {
                form.Original = await api.OfficialResult(entry, login, cancellationToken);
                form.ResultStatus = form.Original.ResultStatus; form.FinishTime = form.Original.FinishTimeSeconds is null ? null : form.Original.FinishTime;
                form.OverallPosition = form.Original.OverallPosition; form.CategoryPosition = form.Original.CategoryPosition; form.Notes = form.Original.Notes;
            }
            else if (entry.Result is not null) return RedirectToAction(nameof(Details), new { eventId, enrolmentId });
            return View("Form", form);
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    private async Task<IActionResult> Save(int eventId, int enrolmentId, ResultForm form, bool edit, CancellationToken cancellationToken)
    {
        bool submitted = false;
        try
        {
            var login = await Login(); form.Entry = await Entry(eventId, enrolmentId, login, cancellationToken);
            if (edit) form.Original = await api.OfficialResult(form.Entry, login, cancellationToken);
            if (ModelState.IsValid)
            {
                submitted = true; await api.SaveOfficialResult(form, login, cancellationToken);
                TempData["ResultSaved"] = true;
                return RedirectToAction(nameof(Details), new { eventId, enrolmentId });
            }
        }
        catch (EventWriteException error) when (form.Entry is not null && error.StatusCode is 400 or 409)
        {
            bool added = false;
            foreach (var field in error.Errors)
                foreach (string message in field.Value ?? [])
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        string key = field.Key.Equals("FinishTimeSeconds", StringComparison.OrdinalIgnoreCase) ? "FinishTime" :
                            new[] { "ResultStatus", "OverallPosition", "CategoryPosition", "Notes", "FinishTime" }.FirstOrDefault(key => key.Equals(field.Key, StringComparison.OrdinalIgnoreCase)) ?? "";
                        ModelState.AddModelError(key, message); added = true;
                    }
            if (!added) ModelState.AddModelError("", error.Message);
            Response.StatusCode = error.StatusCode;
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View("Form", form);
    }

    private async Task<EntryDetails> Entry(int eventId, int enrolmentId, WebLogin login, CancellationToken cancellationToken)
    {
        await api.OwnedEvent(eventId, login, cancellationToken);
        return await api.RosterDetails(eventId, enrolmentId, login, cancellationToken);
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
        logger.LogWarning(error, "The official-result request could not be completed.");
        ViewData["UnconfirmedResult"] = write; Response.StatusCode = 503;
        return View("Unavailable");
    }
}
