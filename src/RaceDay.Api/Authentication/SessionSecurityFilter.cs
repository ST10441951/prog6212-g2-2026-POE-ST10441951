using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Authentication;

// Swagger document filters: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Describes the session cookie on protected Swagger operations.</summary>
public sealed class SessionSecurityFilter : IDocumentFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var description in context.ApiDescriptions)
        {
            bool optionalSession = description.ActionDescriptor.EndpointMetadata.OfType<OptionalSessionAttribute>().Any();
            if (description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any() && !optionalSession)
                continue;

            string path = "/" + description.RelativePath?.Split('?')[0];
            var operation = document.Paths[path].Operations!
                .Single(x => string.Equals(x.Key.ToString(), description.HttpMethod, StringComparison.OrdinalIgnoreCase)).Value;
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("SessionCookie", document, null)] = []
                }
            ];
            if (optionalSession)
                operation.Security.Insert(0, new OpenApiSecurityRequirement());
        }
    }
}
