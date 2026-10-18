using SkiaSharp;
using System.Buffers.Binary;

namespace RaceDay.Api.Storage;

// Upload limits and generated names: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// Header inspection, full decoding and re-encoding: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcodec
// https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skimage.encode
// Accessed: 18 October 2026.
internal static class ImageValidation
{
    internal const long FileLimit = 5 * 1024 * 1024;
    internal const long RequestLimit = FileLimit + 64 * 1024;
    private static readonly SemaphoreSlim Decoders = new(2);

    internal static async Task<ValidatedImage> Read(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) throw new InvalidImageException(400, "Choose a non-empty image.");
        if (file.Length > FileLimit) throw new InvalidImageException(413, "Images must be at most 5 MiB.");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string? expected = extension switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", _ => null };
        if (expected is null || !string.Equals(file.ContentType, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidImageException(415, "Use a JPEG or PNG with its matching file extension and MIME type.");
        await Decoders.WaitAsync(cancellationToken);
        try
        {
            await using var input = new MemoryStream();
            await file.CopyToAsync(input, cancellationToken);
            if (input.Length > FileLimit) throw new InvalidImageException(413, "Images must be at most 5 MiB.");
            byte[] bytes = input.ToArray();
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null)
            {
                bool header = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                    bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
                throw new InvalidImageException(header ? 400 : 415, header ? "The image is damaged." : "The content is not a supported JPEG or PNG.");
            }
            string? detected = codec.EncodedFormat switch
            {
                SKEncodedImageFormat.Png => "image/png", SKEncodedImageFormat.Jpeg => "image/jpeg", _ => null
            };
            if (detected != expected) throw new InvalidImageException(415, "The image content does not match its declared type.");
            if (detected == "image/png" && HasAnimationChunk(bytes))
                throw new InvalidImageException(400, "Use a still image rather than an animated PNG.");
            if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || codec.Info.Width > 4096 || codec.Info.Height > 4096 ||
                (long)codec.Info.Width * codec.Info.Height * 4 > 64 * 1024 * 1024 || codec.FrameCount > 1)
                throw new InvalidImageException(400, "Use one still image, at most 4096 pixels per side and 64 MiB of decoded pixels.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
                throw new InvalidImageException(400, "The image is damaged or cannot be fully decoded.");
            cancellationToken.ThrowIfCancellationRequested();
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(codec.EncodedFormat, 90);
            if (encoded is null) throw new InvalidImageException(400, "The image cannot be processed.");
            if (encoded.Size > FileLimit) throw new InvalidImageException(413, "The processed image exceeds 5 MiB; use a smaller image.");
            return new ValidatedImage(encoded.ToArray(), expected, expected == "image/png" ? "png" : "jpg");
        }
        finally { Decoders.Release(); }
    }

    // PNG chunks use a big-endian length and four-byte type.
    // W3C PNG specification: https://www.w3.org/TR/png-3/#5Chunk-layout
    private static bool HasAnimationChunk(byte[] bytes)
    {
        int offset = 8;
        while (offset <= bytes.Length - 12)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > bytes.Length - offset - 12) return false;
            if (bytes.AsSpan(offset + 4, 4).SequenceEqual("acTL"u8)) return true;
            if (bytes.AsSpan(offset + 4, 4).SequenceEqual("IEND"u8)) return false;
            offset += (int)length + 12;
        }
        return false;
    }
}

internal sealed record ValidatedImage(byte[] Content, string ContentType, string Extension);
internal sealed class InvalidImageException(int status, string message) : Exception(message)
{
    internal int Status { get; } = status;
}
