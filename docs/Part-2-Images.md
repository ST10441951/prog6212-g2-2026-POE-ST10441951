# RaceDay Part 2 - Images

## 1. Endpoints

| Method | Route | Success |
|---|---|---|
| PUT | `/api/profile/picture` | 200 with `profilePictureUrl` |
| DELETE | `/api/profile/picture` | 204 |
| PUT | `/api/events/{eventId}/banner` | 200 with `bannerImageUrl` |
| DELETE | `/api/events/{eventId}/banner` | 204 |

Pictures use the signed-in Participant. Banners require the event's Organiser. Missing sessions return 401, wrong roles/owners 403 and missing events 404 (Microsoft, 2026a).

## 2. File rules

- Send exactly one `imageFile` as multipart form data, with no extra fields.
- Accept JPEG (.jpg/.jpeg, image/jpeg) or PNG (.png, image/png). Content must match its name and MIME type.
- Input and processed files: maximum 5 MiB. Whole request: another 64 KiB for form headers. Oversized uploads, including streamed bodies, return 413.
- Still images only, maximum 4096 pixels per side and 64 MiB decoded pixels.
- Missing, empty, damaged, animated or oversized-dimension images return 400. Unsupported formats/mismatches return 415.
- SkiaSharp fully decodes and re-encodes the image, removing metadata/trailing data. JPEG uses quality 90; at most two decoders run together (Microsoft, 2026b; 2026c; 2026d; 2026i; 2026j; W3C, 2025).

## 3. Storage

Blobs use a private container. SQL stores generated names, not files or URLs. Responses supply read-only, one-hour SAS URLs; GET refreshes them. Credentials stay in API settings (Microsoft, 2026e; 2026f).

Replacement uploads the new file, saves its name, then removes the old file. A failed SQL save attempts to remove the new upload. Cleanup failures are logged. SQL and Blob Storage do not share a transaction.

DELETE removes the blob and clears its SQL name; repeated DELETE returns 204. Storage failures return 503 and keep the name for retry. If SQL fails after deletion, retrying clears the missing-blob reference. Row/event locks protect competing writes (Microsoft, 2026g).

## 4. Setup

Start Azurite from the repository folder in a separate terminal (Microsoft, 2026h):

```text
npx --yes --package azurite@3.37.0 azurite-blob --blobHost 127.0.0.1 --location .azurite --silent
```

Development uses `UseDevelopmentStorage=true` and `raceday-images`. Keep the emulator running for images and tests. Its files are ignored by Git.

For Azure, privately set `ImageStorage__ConnectionString` to an account-key connection and optionally `ImageStorage__ContainerName`. Use a private container; real keys must not be committed. Tests use their own `raceday-tests-...` container; `RACEDAY_TEST_STORAGE_CONNECTION` selects another test account.

## 5. Checks and evidence

The 76 image checks cover validation, private/signed reads, saved names, replacement/removal, access, failures and concurrency. Swagger's 40 operations are compared with the endpoint plan. Current results are in the [rubric review](Part-2-Rubric-Review.md).

Swagger/HTTP checks on 18 October uploaded and displayed both images, replaced and removed them, and confirmed 413 for known-length and streamed 6 MiB requests. Temporary records were removed; sample data was kept.

| Evidence | File |
|---|---|
| Picture upload | [Swagger picture](evidence/swagger-picture-upload.jpg) |
| Saved picture | [Picture read](evidence/profile-picture-read.jpg) |
| Banner upload | [Swagger banner](evidence/swagger-banner-upload.jpg) |
| Saved banner | [Banner read](evidence/event-banner-read.jpg) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026b) 'Upload files in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026c) 'SKCodec Class', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcodec (Accessed: 18 October 2026).

Microsoft (2026d) 'SKImage.Encode Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skimage.encode (Accessed: 18 October 2026).

Microsoft (2026e) 'Upload a blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload (Accessed: 18 October 2026).

Microsoft (2026f) 'Create a service SAS for a container or blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/sas-service-create-dotnet (Accessed: 18 October 2026).

Microsoft (2026g) 'Using Transactions', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/ef/core/saving/transactions (Accessed: 18 October 2026).

Microsoft (2026h) 'Use the Azurite emulator for local Azure Storage development', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 18 October 2026).

Microsoft (2026i) 'Filters in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0 (Accessed: 18 October 2026).

Microsoft (2026j) 'HttpRequestRewindExtensions.EnableBuffering Method', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httprequestrewindextensions.enablebuffering?view=aspnetcore-10.0 (Accessed: 18 October 2026).

W3C (2025) *Portable Network Graphics (PNG) Specification (Third Edition)*. Available at: https://www.w3.org/TR/png-3/ (Accessed: 18 October 2026).
