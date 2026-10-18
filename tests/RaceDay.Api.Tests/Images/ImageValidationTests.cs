using System.Buffers.Binary;
using System.IO.Hashing;
using System.Net;
using System.Text;
using Xunit;

namespace RaceDay.Api.Tests.Images;

// Invalid multipart requests and image fixtures: Microsoft (2026), W3C (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// https://www.w3.org/TR/png-3/#5Chunk-layout
// Accessed: 18 October 2026.
public sealed partial class ImageTests
{
    [Theory]
    [InlineData(false, "empty", 400)]
    [InlineData(true, "empty", 400)]
    [InlineData(false, "large", 413)]
    [InlineData(true, "large", 413)]
    [InlineData(false, "text", 415)]
    [InlineData(true, "text", 415)]
    [InlineData(false, "extension", 415)]
    [InlineData(true, "extension", 415)]
    [InlineData(false, "mime", 415)]
    [InlineData(true, "mime", 415)]
    [InlineData(false, "mismatch", 415)]
    [InlineData(true, "mismatch", 415)]
    [InlineData(false, "damaged", 400)]
    [InlineData(true, "damaged", 400)]
    [InlineData(false, "wide", 400)]
    [InlineData(true, "wide", 400)]
    [InlineData(false, "tall", 400)]
    [InlineData(true, "tall", 400)]
    [InlineData(false, "animated", 400)]
    [InlineData(true, "animated", 400)]
    public async Task InvalidImages_DoNotWriteToSqlOrStorage(bool banner, string scenario, int expected)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        byte[] bytes = scenario switch
        {
            "empty" => [], "large" => new byte[5 * 1024 * 1024 + 1], "text" => Encoding.UTF8.GetBytes("This is not an image."),
            "mismatch" => Pixels("jpg"), "damaged" => Pixels()[..50], "wide" => Pixels(width: 4097),
            "tall" => Pixels(height: 4097), "animated" => AddChunk(Pixels(), "acTL", [0, 0, 0, 2, 0, 0, 0, 0]), _ => Pixels()
        };
        using var form = Form(bytes, scenario == "extension" ? "image.svg" : "image.png", scenario == "mime" ? "image/jpeg" : "image/png");
        using var response = await client.PutAsync(Path(banner), form);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Null(await SavedName(banner));
        Assert.Empty(await Objects());
    }

    [Theory]
    [InlineData(false, "missing")]
    [InlineData(true, "missing")]
    [InlineData(false, "wrong-name")]
    [InlineData(true, "wrong-name")]
    [InlineData(false, "multiple")]
    [InlineData(true, "multiple")]
    [InlineData(false, "userId")]
    [InlineData(true, "eventId")]
    [InlineData(false, "profilePictureBlobName")]
    [InlineData(true, "bannerImageBlobName")]
    public async Task Form_RequiresOneNamedFileAndRejectsExtraFields(bool banner, string scenario)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var form = scenario == "missing" ? new MultipartFormDataContent() : Form(Pixels(), field: scenario == "wrong-name" ? "otherFile" : "imageFile");
        if (scenario == "multiple") form.Add(new ByteArrayContent(Pixels()), "imageFile", "other.png");
        else if (scenario is not ("missing" or "wrong-name")) form.Add(new StringContent("99"), scenario);
        using var response = await client.PutAsync(Path(banner), form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(await SavedName(banner));
        Assert.Empty(await Objects());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_RejectsJsonInsteadOfMultipart(bool banner)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var response = await client.PutAsync(Path(banner), new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Empty(await Objects());
    }

    [Fact]
    public async Task Processing_RemovesEmbeddedTextMetadataAndTrailingContent()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, false);
        const string privateText = "PRIVATE-IMAGE-METADATA";
        var bytes = AddChunk(Pixels(), "tEXt", Encoding.UTF8.GetBytes("Comment\0" + privateText));
        bytes = [.. bytes, .. Encoding.UTF8.GetBytes("TRAILING-CONTENT")];
        using var form = Form(bytes);
        using var response = await client.PutAsync(Path(false), form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = (await storage.Container.GetBlobClient((await SavedName(false))!).DownloadContentAsync()).Value.Content.ToArray();
        Assert.DoesNotContain(privateText, Encoding.UTF8.GetString(stored));
        Assert.DoesNotContain("TRAILING-CONTENT", Encoding.UTF8.GetString(stored));
    }

    [Fact]
    public async Task A4096PixelImage_IsAcceptedAtTheDimensionBoundary()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, false);
        using var form = Form(Pixels(width: 4096, height: 1));
        using var response = await client.PutAsync(Path(false), form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static byte[] AddChunk(byte[] png, string type, byte[] content)
    {
        byte[] chunk = new byte[12 + content.Length];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0, 4), (uint)content.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(chunk, 4);
        content.CopyTo(chunk, 8);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(chunk.Length - 4), Crc32.HashToUInt32(chunk.AsSpan(4, 4 + content.Length)));
        return [.. png.AsSpan(0, 33), .. chunk, .. png.AsSpan(33)];
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OversizedRequest_Returns413BeforeBindingForKnownAndStreamedLengths(bool banner, bool chunked)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var request = new HttpRequestMessage(HttpMethod.Put, Path(banner)) { Content = Form(new byte[6 * 1024 * 1024]) };
        if (chunked) request.Headers.TransferEncodingChunked = true;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(await SavedName(banner));
        Assert.Empty(await Objects());
    }
}
