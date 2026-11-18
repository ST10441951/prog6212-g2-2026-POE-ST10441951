using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models.Entries;

// Form binding and read-only API responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Enrolments/.
// Accessed: 18 November 2026.
public sealed class EntryForm
{
    [Required(ErrorMessage = "Choose a category."), Range(1, int.MaxValue), Display(Name = "Category")]
    public int? EventCategoryId { get; set; }

    [BindNever, ValidateNever]
    public EventDetails? Event { get; set; }
}

public sealed class EntrySearch
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    public string? Status { get; set; }
}

public sealed class EntryListViewModel
{
    public EntrySearch Search { get; init; } = new();
    public EntryPage? Entries { get; init; }
    public string? Error { get; init; }
}

public sealed class EntryPage
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required IReadOnlyList<EntryDetails> Items { get; init; }
}

public sealed class EntryDetails
{
    public required int EnrolmentId { get; init; }
    public required int ParticipantUserId { get; init; }
    public required string ParticipantName { get; init; }
    public required int EventId { get; init; }
    public required string EventName { get; init; }
    public required DateTimeOffset EventDateTime { get; init; }
    public required int EventCategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal CategoryDistanceKm { get; init; }
    public required DateTimeOffset EnrolmentDate { get; init; }
    public required string Status { get; init; }
    public string? BibNumber { get; init; }
}
