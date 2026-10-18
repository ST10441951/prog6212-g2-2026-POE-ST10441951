using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Storage;

// Resource filters run before model binding; buffering allows the form binder to read the body again.
// Microsoft (2026): https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httprequestrewindextensions.enablebuffering
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Return 413 before multipart binding converts an oversized body into a validation error.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ImageUploadLimitAttribute : Attribute, IAsyncResourceFilter
{
    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.ContentLength > ImageValidation.RequestLimit) { Reject(context); return; }
        request.EnableBuffering(bufferThreshold: 64 * 1024);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);
        try
        {
            long length = 0;
            int read;
            while ((read = await request.Body.ReadAsync(buffer, context.HttpContext.RequestAborted)) != 0)
            {
                length += read;
                if (length > ImageValidation.RequestLimit) { Reject(context); return; }
            }
            request.Body.Position = 0;
        }
        catch (BadHttpRequestException error) when (error.StatusCode == 413) { Reject(context); return; }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
        await next();
    }

    private static void Reject(ResourceExecutingContext context) => context.Result = new ObjectResult(new ProblemDetails
    {
        Status = 413, Title = "The image upload request is too large",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.14"
    }) { StatusCode = 413, ContentTypes = { "application/problem+json" } };
}
