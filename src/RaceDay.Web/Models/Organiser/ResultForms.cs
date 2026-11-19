using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Web.Models.Entries;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Results;

namespace RaceDay.Web.Models.Organiser;

// Form binding and read-only page context: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Results/.
// Accessed: 19 November 2026.
public sealed class EventResultSearch
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    public string? ResultStatus { get; set; }
    [Range(1, int.MaxValue)]
    public int? EventCategoryId { get; set; }
}

public sealed class EventResultList
{
    public required EventDetails Event { get; init; }
    public EventResultSearch Search { get; init; } = new();
    public ResultPage? Results { get; init; }
    public string? Error { get; init; }
}

public sealed class ResultForm
{
    [Required, StringLength(30), Display(Name = "Result status")]
    public string ResultStatus { get; set; } = "Completed";
    [StringLength(20), Display(Name = "Finish time (HH:MM:SS)")]
    public string? FinishTime { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Overall position")]
    public int? OverallPosition { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Category position")]
    public int? CategoryPosition { get; set; }
    [StringLength(500), Display(Name = "Organiser notes")]
    public string? Notes { get; set; }
    [BindNever, ValidateNever]
    public EntryDetails? Entry { get; set; }
    [BindNever, ValidateNever]
    public ResultDetails? Original { get; set; }
}
