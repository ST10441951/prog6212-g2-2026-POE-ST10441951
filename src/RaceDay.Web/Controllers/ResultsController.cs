using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Results;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Private role-restricted reads and query validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Participant")]
[Route("results")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ResultsController(ResultApiClient results, WebSessionStore sessions, ILogger<ResultsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ResultSearch search, CancellationToken cancellationToken)
    {
        search.ResultStatus = string.IsNullOrWhiteSpace(search.ResultStatus) ? null : search.ResultStatus.Trim();
        if (!ModelState.IsValid) return InvalidSearch(search);
        try
        {
            return View(new ResultListViewModel { Search = search, Results = await results.Mine(search, await Login(), cancellationToken) });
        }
        catch (ResultRequestException error) when (error.StatusCode == 400) { return InvalidSearch(search); }
        catch (Exception error) when (error is ApiSessionEndedException or ResultRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    [HttpGet("{enrolmentId:int:min(1)}")]
    public async Task<IActionResult> Details([FromRoute] int enrolmentId, CancellationToken cancellationToken)
    {
        try { return View(await results.Details(enrolmentId, await Login(), cancellationToken)); }
        catch (Exception error) when (error is ApiSessionEndedException or ResultRequestException or ApiUnavailableException)
        { return await Failure(error); }
    }

    private IActionResult InvalidSearch(ResultSearch search)
    {
        Response.StatusCode = 400;
        return View("Index", new ResultListViewModel { Search = search, Error = "Choose a valid result status and page number." });
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();

    private async Task<IActionResult> Failure(Exception error)
    {
        if (error is ApiSessionEndedException)
        {
            await sessions.RemoveAsync(HttpContext);
            return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
        }
        if (error is ResultRequestException { StatusCode: 403 }) return Forbid();
        if (error is ResultRequestException { StatusCode: 404 }) return NotFound();
        logger.LogWarning(error, "The personal results could not be loaded.");
        Response.StatusCode = 503;
        return View("Unavailable");
    }
}
