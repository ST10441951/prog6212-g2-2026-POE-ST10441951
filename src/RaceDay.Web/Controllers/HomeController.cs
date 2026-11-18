using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Controllers and typed API services: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AllowAnonymous]
public sealed class HomeController(RaceDayApiClient api, ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventSearch search, CancellationToken cancellationToken)
    {
        search.EventType = string.IsNullOrWhiteSpace(search.EventType) ? null : search.EventType.Trim();
        search.Province = string.IsNullOrWhiteSpace(search.Province) ? null : search.Province.Trim();
        if (!ModelState.IsValid) return InvalidFilters(search);
        try
        {
            var page = await api.GetUpcomingEvents(search, cancellationToken);
            return View(new HomeViewModel
            {
                Events = page.Items, TotalCount = page.TotalCount, TotalPages = page.TotalPages, Search = search
            });
        }
        catch (ApiFilterException)
        {
            return InvalidFilters(search);
        }
        catch (ApiUnavailableException error)
        {
            logger.LogWarning(error, "The upcoming events could not be loaded.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new HomeViewModel
            {
                ErrorMessage = "Events are temporarily unavailable. Please try again shortly.",
                Search = search
            });
        }
    }

    private IActionResult InvalidFilters(EventSearch search)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", new HomeViewModel
        {
            Search = search,
            ErrorMessage = "Choose an event type and province from the available options, valid dates and a page number of at least 1. The final date must not be earlier than the first."
        });
    }
}
