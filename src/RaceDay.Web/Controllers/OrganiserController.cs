using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Role-protected MVC actions: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Organiser")]
[Route("organiser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrganiserController(OrganiserApiClient api, WebSessionStore sessions, ILogger<OrganiserController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try { return View(await api.Dashboard(await Login(), cancellationToken)); }
        catch (ApiSessionEndedException) { return await SignInAgain(); }
        catch (Exception error) when (error is ApiUnavailableException or OrganiserFilterException) { return Unavailable(error); }
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] OwnedEventSearch search, CancellationToken cancellationToken)
    {
        search.Status = string.IsNullOrWhiteSpace(search.Status) ? null : search.Status.Trim();
        if (!ModelState.IsValid) return InvalidSearch(search);
        try { return View(new OwnedEventListViewModel { Search = search, Events = await api.Events(search, await Login(), cancellationToken) }); }
        catch (OrganiserFilterException) { return InvalidSearch(search); }
        catch (ApiSessionEndedException) { return await SignInAgain(); }
        catch (ApiUnavailableException error) { return Unavailable(error); }
    }

    private IActionResult InvalidSearch(OwnedEventSearch search)
    {
        Response.StatusCode = 400;
        return View("Events", new OwnedEventListViewModel { Search = search, Error = "Choose a valid event status and page number." });
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();
    private async Task<IActionResult> SignInAgain()
    {
        await sessions.RemoveAsync(HttpContext);
        return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
    }

    private IActionResult Unavailable(Exception error)
    {
        logger.LogWarning(error, "The Organiser page could not be loaded.");
        Response.StatusCode = 503;
        return View("Unavailable");
    }
}
