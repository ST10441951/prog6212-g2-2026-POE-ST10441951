using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RaceDay.Web.Models.Images;

// Multipart binding and separate read-only context: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// Accessed: 19 November 2026.
public sealed class ImagePage
{
    public int? EventId { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public bool Picture => EventId is null;
}

public sealed class ImageUploadForm
{
    [Required, Display(Name = "Image file")]
    public IFormFile? ImageFile { get; set; }
    [BindNever, ValidateNever]
    public ImagePage? Page { get; set; }
}
