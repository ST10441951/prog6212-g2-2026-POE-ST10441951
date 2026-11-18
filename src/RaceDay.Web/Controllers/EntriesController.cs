using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Role checks, form validation and redirect-after-post: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Participant")]
[Route("entries")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EntriesController(EntryApiClient entries, RaceDayApiClient events, WebSessionStore sessions,
    ILogger<EntriesController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] EntrySearch search, CancellationToken cancellationToken)
    {
        search.Status = string.IsNullOrWhiteSpace(search.Status) ? null : search.Status.Trim();
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return View(new EntryListViewModel { Search = search, Error = "Choose a valid status and page number." });
        }
        try
        {
            var page = await entries.Mine(search, await Login(), cancellationToken);
            return View(new EntryListViewModel { Search = search, Entries = page });
        }
        catch (EntryRequestException error) when (error.StatusCode == 400)
        {
            Response.StatusCode = 400;
            return View(new EntryListViewModel { Search = search, Error = "Choose Confirmed, Cancelled or all entries." });
        }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}")]
    public async Task<IActionResult> Details([FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try { return View(await entries.Details(enrolmentId, await Login(), cancellationToken)); }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpGet("/events/{eventId:int:min(1)}/enter")]
    public async Task<IActionResult> Enter([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        try { return await EntryForm(eventId, new EntryForm(), cancellationToken); }
        catch (ApiUnavailableException error) { return await Failure(error); }
    }

    [HttpPost("/events/{eventId:int:min(1)}/enter")]
    public async Task<IActionResult> Enter([FromRoute] int eventId, [Bind("EventCategoryId")] EntryForm model, CancellationToken cancellationToken)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var entry = await entries.Create(eventId, model.EventCategoryId!.Value, await Login(), cancellationToken);
                TempData["EntrySaved"] = true;
                return RedirectToAction(nameof(Details), new { enrolmentId = entry.EnrolmentId });
            }
            Response.StatusCode = 400;
            return await EntryForm(eventId, model, cancellationToken);
        }
        catch (EntryRequestException error) when (error.StatusCode is 400 or 404 or 409)
        {
            Response.StatusCode = error.StatusCode;
            ModelState.AddModelError("", error.Message);
            try { return await EntryForm(eventId, model, cancellationToken); }
            catch (ApiUnavailableException unavailable) { return await Failure(unavailable); }
        }
        catch (ApiUnavailableException error)
        {
            logger.LogWarning(error, "The entry submission could not be confirmed.");
            Response.StatusCode = 503;
            ViewData["UnconfirmedEntry"] = true;
            return View("Unavailable");
        }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException)
        { return await Failure(error); }
    }

    private async Task<IActionResult> EntryForm(int eventId, EntryForm model, CancellationToken cancellationToken)
    {
        model.Event = await events.GetEvent(eventId, cancellationToken);
        return model.Event is null ? NotFound() : View("Enter", model);
    }

    [HttpGet("{enrolmentId:int:min(1)}/cancel")]
    public Task<IActionResult> Cancel([FromRoute] int enrolmentId, CancellationToken cancellationToken) =>
        Confirmation(enrolmentId, "Cancel", cancellationToken);

    [HttpPost("{enrolmentId:int:min(1)}/cancel")]
    public Task<IActionResult> CancelEntry([FromRoute] int enrolmentId, CancellationToken cancellationToken) =>
        ChangeEntry(enrolmentId, "Cancel", cancellationToken);

    [HttpGet("{enrolmentId:int:min(1)}/delete")]
    public Task<IActionResult> Delete([FromRoute] int enrolmentId, CancellationToken cancellationToken) =>
        Confirmation(enrolmentId, "Delete", cancellationToken);

    [HttpPost("{enrolmentId:int:min(1)}/delete")]
    public Task<IActionResult> DeleteEntry([FromRoute] int enrolmentId, CancellationToken cancellationToken) =>
        ChangeEntry(enrolmentId, "Delete", cancellationToken);

    [HttpGet("{enrolmentId:int:min(1)}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await entries.Details(enrolmentId, await Login(), cancellationToken);
            return await ReactivationForm(entry, new ReactivateEntryForm { EventCategoryId = entry.EventCategoryId }, cancellationToken);
        }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpPost("{enrolmentId:int:min(1)}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] int enrolmentId, [Bind("EventCategoryId")] ReactivateEntryForm model,
        CancellationToken cancellationToken)
    {
        EntryDetails? saved = null;
        bool submitted = false;
        try
        {
            var login = await Login();
            saved = await entries.Details(enrolmentId, login, cancellationToken);
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return await ReactivationForm(saved, model, cancellationToken);
            }
            submitted = true;
            await entries.Reactivate(saved, model.EventCategoryId!.Value, login, cancellationToken);
            TempData["EntryMessage"] = "Your entry has been reactivated.";
            return RedirectToAction(nameof(Details), new { enrolmentId });
        }
        catch (EntryRequestException error) when (saved is not null && error.StatusCode is 400 or 404 or 409)
        {
            Response.StatusCode = error.StatusCode;
            ModelState.AddModelError("", error.Message);
            try { return await ReactivationForm(saved, model, cancellationToken); }
            catch (ApiUnavailableException unavailable) { return await Failure(unavailable); }
        }
        catch (ApiUnavailableException error) { return submitted ? UnconfirmedChange(error) : await Failure(error); }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException)
        { return await Failure(error); }
    }

    private async Task<IActionResult> Confirmation(int enrolmentId, string action, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await entries.Details(enrolmentId, await Login(), cancellationToken);
            return View("Confirm", new EntryConfirmation { Entry = entry, Action = action });
        }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    private async Task<IActionResult> ChangeEntry(int enrolmentId, string action, CancellationToken cancellationToken)
    {
        EntryDetails? saved = null;
        bool submitted = false;
        try
        {
            var login = await Login();
            saved = await entries.Details(enrolmentId, login, cancellationToken);
            submitted = true;
            if (action == "Delete")
            {
                await entries.Delete(enrolmentId, login, cancellationToken);
                TempData["EntryMessage"] = "Your entry has been deleted.";
                return RedirectToAction(nameof(Index));
            }
            await entries.Cancel(saved, login, cancellationToken);
            TempData["EntryMessage"] = "Your entry has been cancelled.";
            return RedirectToAction(nameof(Details), new { enrolmentId });
        }
        catch (EntryRequestException error) when (saved is not null && error.StatusCode is 400 or 404 or 409)
        {
            Response.StatusCode = error.StatusCode;
            ModelState.AddModelError("", error.Message);
            return View("Confirm", new EntryConfirmation { Entry = saved, Action = action });
        }
        catch (ApiUnavailableException error) { return submitted ? UnconfirmedChange(error) : await Failure(error); }
        catch (Exception error) when (error is ApiSessionEndedException or EntryRequestException)
        { return await Failure(error); }
    }

    private async Task<IActionResult> ReactivationForm(EntryDetails entry, ReactivateEntryForm model, CancellationToken cancellationToken)
    {
        model.Entry = entry;
        model.Event = await events.GetEvent(entry.EventId, cancellationToken);
        return View("Reactivate", model);
    }

    private IActionResult UnconfirmedChange(ApiUnavailableException error)
    {
        logger.LogWarning(error, "The entry change could not be confirmed.");
        Response.StatusCode = 503;
        ViewData["UnconfirmedChange"] = true;
        return View("Unavailable");
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();

    private async Task<IActionResult> Failure(Exception error)
    {
        if (error is ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
        }
        if (error is EntryRequestException { StatusCode: 403 }) return Forbid();
        if (error is EntryRequestException { StatusCode: 404 }) return NotFound();
        logger.LogWarning(error, "The entry page could not be loaded.");
        Response.StatusCode = 503;
        return View("Unavailable");
    }
}
