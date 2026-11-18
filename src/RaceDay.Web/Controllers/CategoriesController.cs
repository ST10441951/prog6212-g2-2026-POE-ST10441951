using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Organiser;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Role restrictions, allowed form fields and antiforgery: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int:min(1)}/categories")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriesController(OrganiserApiClient api, WebSessionStore sessions, ILogger<CategoriesController> logger) : Controller
{
    private const string CategoryFields = "CategoryName,DistanceKm,MinimumAge,MaximumAge,EntryFee,Capacity,IsAvailable";
    private const string RouteFields = "RouteName,StartLocation,FinishLocation,RouteDescription,RouteMapUrl,ElevationGainMetres";

    [HttpGet("")]
    public async Task<IActionResult> Index([FromRoute] int eventId, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Login(); var item = await api.OwnedEvent(eventId, login, cancellationToken);
            return View(new CategoryListPage { Event = item, Categories = await api.OwnedCategories(eventId, login, cancellationToken) });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("create")]
    public Task<IActionResult> Create([FromRoute] int eventId, CancellationToken cancellationToken) => CategoryFormPage(eventId, null, cancellationToken);
    [HttpGet("{categoryId:int:min(1)}/edit")]
    public Task<IActionResult> Edit([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => CategoryFormPage(eventId, categoryId, cancellationToken);
    [HttpPost("create")]
    public Task<IActionResult> Create([FromRoute] int eventId, [Bind(CategoryFields)] CategoryForm model, CancellationToken cancellationToken) => SaveCategory(eventId, null, model, cancellationToken);
    [HttpPost("{categoryId:int:min(1)}/edit")]
    public Task<IActionResult> Edit([FromRoute] int eventId, [FromRoute] int categoryId, [Bind(CategoryFields)] CategoryForm model, CancellationToken cancellationToken) => SaveCategory(eventId, categoryId, model, cancellationToken);

    private async Task<IActionResult> CategoryFormPage(int eventId, int? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var item = await Context(eventId, categoryId, await Login(), cancellationToken);
            ViewData["Editing"] = categoryId is not null;
            return View("CategoryForm", item.Category is null ? new CategoryForm() : CategoryForm.From(item.Category));
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    private async Task<IActionResult> SaveCategory(int eventId, int? categoryId, CategoryForm model, CancellationToken cancellationToken)
    {
        bool submitted = false; ViewData["Editing"] = categoryId is not null;
        try
        {
            var login = await Login(); await Context(eventId, categoryId, login, cancellationToken);
            if (ModelState.IsValid)
            {
                submitted = true; await api.SaveCategory(eventId, categoryId, model, login, cancellationToken);
                TempData["CategorySaved"] = categoryId is null ? "Your category has been created." : "Your category has been updated.";
                return RedirectToAction(nameof(Index), new { eventId });
            }
        }
        catch (EventWriteException error) when (error.StatusCode is 400 or 409) { AddErrors(error); }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View("CategoryForm", model);
    }

    [HttpGet("{categoryId:int:min(1)}/route")]
    public async Task<IActionResult> RouteDetails([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var context = await Context(eventId, categoryId, await Login(), cancellationToken);
            return View(new CategoryPage { Event = context.Event, Category = context.Category! });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    [HttpGet("{categoryId:int:min(1)}/route/create")]
    public Task<IActionResult> RouteCreate([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => RouteFormPage(eventId, categoryId, false, cancellationToken);
    [HttpGet("{categoryId:int:min(1)}/route/edit")]
    public Task<IActionResult> RouteEdit([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => RouteFormPage(eventId, categoryId, true, cancellationToken);
    [HttpPost("{categoryId:int:min(1)}/route/create")]
    public Task<IActionResult> RouteCreate([FromRoute] int eventId, [FromRoute] int categoryId, [Bind(RouteFields)] RouteForm model, CancellationToken cancellationToken) => SaveRoute(eventId, categoryId, model, false, cancellationToken);
    [HttpPost("{categoryId:int:min(1)}/route/edit")]
    public Task<IActionResult> RouteEdit([FromRoute] int eventId, [FromRoute] int categoryId, [Bind(RouteFields)] RouteForm model, CancellationToken cancellationToken) => SaveRoute(eventId, categoryId, model, true, cancellationToken);

    private async Task<IActionResult> RouteFormPage(int eventId, int categoryId, bool editing, CancellationToken cancellationToken)
    {
        try
        {
            var context = await Context(eventId, categoryId, await Login(), cancellationToken);
            var route = context.Category!.Route;
            if (editing && route is null) return NotFound();
            if (!editing && route is not null) return RedirectToAction(nameof(RouteDetails), new { eventId, categoryId });
            ViewData["RouteEditing"] = editing;
            return View("RouteForm", editing ? RouteForm.From(route!) : new RouteForm());
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    private async Task<IActionResult> SaveRoute(int eventId, int categoryId, RouteForm model, bool editing, CancellationToken cancellationToken)
    {
        bool submitted = false; ViewData["RouteEditing"] = editing;
        try
        {
            var login = await Login(); var context = await Context(eventId, categoryId, login, cancellationToken);
            if (editing && context.Category!.Route is null) return NotFound();
            if (ModelState.IsValid)
            {
                submitted = true;
                await api.SaveRoute(eventId, categoryId, editing ? context.Category!.Route!.RouteId : null, model, login, cancellationToken);
                TempData["CategorySaved"] = editing ? "Your route has been updated." : "Your route has been created.";
                return RedirectToAction(nameof(RouteDetails), new { eventId, categoryId });
            }
        }
        catch (EventWriteException error) when (error.StatusCode is 400 or 409) { AddErrors(error); }
        catch (Exception error) when (Expected(error)) { return await Failure(error, submitted); }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View("RouteForm", model);
    }

    [HttpGet("{categoryId:int:min(1)}/delete")]
    public Task<IActionResult> Delete([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => Confirmation(eventId, categoryId, false, cancellationToken);
    [HttpGet("{categoryId:int:min(1)}/route/delete")]
    public Task<IActionResult> RouteDelete([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => Confirmation(eventId, categoryId, true, cancellationToken);
    [HttpPost("{categoryId:int:min(1)}/delete")]
    public Task<IActionResult> DeleteConfirmed([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => DeleteSaved(eventId, categoryId, false, cancellationToken);
    [HttpPost("{categoryId:int:min(1)}/route/delete")]
    public Task<IActionResult> RouteDeleteConfirmed([FromRoute] int eventId, [FromRoute] int categoryId, CancellationToken cancellationToken) => DeleteSaved(eventId, categoryId, true, cancellationToken);

    private async Task<IActionResult> Confirmation(int eventId, int categoryId, bool routeOnly, CancellationToken cancellationToken)
    {
        try
        {
            var context = await Context(eventId, categoryId, await Login(), cancellationToken);
            if (routeOnly && context.Category!.Route is null) return NotFound();
            ViewData["DeletingRoute"] = routeOnly;
            return View("Confirm", new CategoryPage { Event = context.Event, Category = context.Category! });
        }
        catch (Exception error) when (Expected(error)) { return await Failure(error); }
    }

    private async Task<IActionResult> DeleteSaved(int eventId, int categoryId, bool routeOnly, CancellationToken cancellationToken)
    {
        CategoryPage? page = null; ViewData["DeletingRoute"] = routeOnly;
        try
        {
            var login = await Login(); var context = await Context(eventId, categoryId, login, cancellationToken);
            if (routeOnly && context.Category!.Route is null) return NotFound();
            page = new CategoryPage { Event = context.Event, Category = context.Category! };
            if (routeOnly) await api.DeleteRoute(eventId, categoryId, login, cancellationToken);
            else await api.DeleteCategory(eventId, categoryId, login, cancellationToken);
            TempData["CategorySaved"] = routeOnly ? "Your route has been removed." : "Your category has been deleted.";
            return RedirectToAction(routeOnly ? nameof(RouteDetails) : nameof(Index), new { eventId, categoryId = routeOnly ? categoryId : (int?)null });
        }
        catch (EventWriteException error) when (page is not null && error.StatusCode == 409)
        { AddErrors(error); return View("Confirm", page); }
        catch (Exception error) when (Expected(error)) { return await Failure(error, page is not null); }
    }

    private async Task<(EventDetails Event, EventCategory? Category)> Context(int eventId, int? categoryId, WebLogin login, CancellationToken cancellationToken)
    {
        var item = await api.OwnedEvent(eventId, login, cancellationToken);
        var category = categoryId is null ? null : item.Categories.SingleOrDefault(value => value.EventCategoryId == categoryId);
        if (categoryId is not null && category is null) throw new EventWriteException(404, "Category not found.");
        ViewData["EventName"] = item.EventName; ViewData["CategoryName"] = category?.CategoryName;
        return (item, category);
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
        logger.LogWarning(error, "The category or route request could not be completed.");
        ViewData["UnconfirmedCategory"] = write; Response.StatusCode = 503;
        return View("Unavailable");
    }

    private void AddErrors(EventWriteException error)
    {
        bool added = false;
        foreach (var entry in error.Errors)
        {
            string key = (CategoryFields + "," + RouteFields).Split(',').FirstOrDefault(field => string.Equals(field, entry.Key, StringComparison.OrdinalIgnoreCase)) ?? "";
            foreach (string message in entry.Value ?? [])
                if (!string.IsNullOrWhiteSpace(message)) { ModelState.AddModelError(key, message); added = true; }
        }
        if (!added) ModelState.AddModelError("", error.Message);
        Response.StatusCode = error.StatusCode;
    }
}
