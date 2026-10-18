using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Contracts.Enrolments;

// Swagger schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Example request fields for entries, reactivation and bib assignment.</summary>
public sealed class EnrolmentExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema example) return;
        if (context.Type == typeof(CreateEnrolmentRequest))
            example.Example = JsonSerializer.SerializeToNode(new { eventId = 3, eventCategoryId = 6 });
        if (context.Type == typeof(ReactivateEnrolmentRequest))
            example.Example = JsonSerializer.SerializeToNode(new { eventCategoryId = 6 });
        if (context.Type == typeof(BibNumberRequest))
            example.Example = JsonSerializer.SerializeToNode(new { bibNumber = "A1042" });
    }
}
