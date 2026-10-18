using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Contracts.Categories;

// Swagger schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Provides a usable category creation and update example.</summary>
public sealed class CategoryExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema example && context.Type == typeof(CategoryWriteRequest))
            example.Example = JsonSerializer.SerializeToNode(new
            {
                categoryName = "10 km Run", distanceKm = 10.00m, minimumAge = 15,
                maximumAge = (int?)null, entryFee = 150.00m, capacity = 100, isAvailable = true
            });
    }
}
