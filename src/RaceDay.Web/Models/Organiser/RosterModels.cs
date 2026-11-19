using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models.Organiser;

// Query binding, explicit form fields and read-only context: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Enrolments/.
// Accessed: 18 November 2026.
public sealed class RosterSearch
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    public string? Status { get; set; }
    [Range(1, int.MaxValue)]
    public int? EventCategoryId { get; set; }
}

public sealed class RosterListPage
{
    public required EventDetails Event { get; init; }
    public RosterSearch Search { get; init; } = new();
    public EntryPage? Entries { get; init; }
    public string? Error { get; init; }
}

public sealed class BibNumberForm
{
    [Required, StringLength(20), Display(Name = "Bib number")]
    public string BibNumber { get; set; } = string.Empty;
    [BindNever, ValidateNever]
    public EntryDetails? Entry { get; set; }
}
