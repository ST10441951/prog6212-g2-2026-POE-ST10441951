namespace RaceDay.Api.Storage;

// Stream-based blob operations: Microsoft (2026).
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload
// Accessed: 18 October 2026.
/// <summary>Private image objects; SQL stores only their generated names.</summary>
public interface IImageStorage
{
    /// <summary>Upload a new object without overwriting another object.</summary>
    Task Upload(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    /// <summary>Delete an object, treating an already missing object as removed.</summary>
    Task Delete(string blobName, CancellationToken cancellationToken);
    /// <summary>A short-lived, read-only URL, or null when no supported name or configuration exists.</summary>
    string? ReadUrl(string? blobName);
}

/// <summary>A storage failure which can be reported without exposing connection details.</summary>
public sealed class ImageStorageException(string message, Exception? innerException = null) : Exception(message, innerException);
