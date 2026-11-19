# RaceDay Part 3 - Pictures and banners

## 1. Pages

Participants use My profile, then Manage profile picture. Organisers use Manage banner on an owned event, or choose an optional banner while creating/editing it. Removal opens a confirmation page before sending a protected POST (Microsoft, 2026a; 2026b).

| MVC page | API request |
|---|---|
| `/profile/picture` | PUT `/api/profile/picture` |
| `/profile/picture/remove` | DELETE `/api/profile/picture` |
| `/organiser/events/{eventId}/banner` | PUT `/api/events/{eventId}/banner` |
| `/organiser/events/{eventId}/banner/remove` | DELETE `/api/events/{eventId}/banner` |

Only the current Participant can change their picture. Banners require the owning Organiser. MVC rechecks the saved account/event before changes; the API repeats access checks. Posted account IDs, event IDs and blob names are not forwarded.

## 2. Uploads and display

MVC forwards one streamed multipart `imageFile` and the current user's API cookie. Antiforgery tokens and other form fields remain in MVC. The API checks JPEG/PNG content, decoding, dimensions and file size. Files must be still images, at most 5 MiB and 4096 pixels per side. MVC also bounds the incoming request and file size (Microsoft, 2026a).

The API generates a blob name, stores it in SQL and returns a temporary read-only URL. MVC has no storage key or Azure SDK. Safe API image URLs display on profiles and event details. Reading the account/event again supplies a fresh URL (Microsoft, 2026c; 2026d).

Replacement removes the old blob after saving the new reference. Removal clears the reference; repeated removal is safe. Keep image and confirmation GETs make no changes. Invalid uploads retain the current image and show the API's reason. The user selects the file again after a rejected form.

Event details are saved before the optional banner upload. If the banner is rejected or cannot be confirmed, MVC opens that saved event's banner page with an explanation. The user can retry the image without creating another event or repeating a successful event edit. A blank banner field keeps the current image.

Missing sessions return to sign-in, other roles/owners are blocked and service failures show a friendly 503. Uncertain changes advise checking the saved image before retrying. No automatic retry repeats an upload or deletion.

## 3. Storage settings

The Azure setup uses storage account `racedayst10441951`, private container `raceday-images`, in South Africa North. The account belongs to Azure for Students. HTTPS is required and anonymous blob access is disabled.

Set `ImageStorage:ConnectionString` and `ImageStorage:ContainerName` in the API project's private user secrets. Its UserSecretsId is `RaceDay-Part2-ST10441951`. In Visual Studio, use Manage User Secrets on RaceDay.Api. Start the API in Development so those settings override the emulator defaults. Private process settings `ImageStorage__ConnectionString` and `ImageStorage__ContainerName` are another option and take precedence. Do not add their real values to MVC or tracked settings (Microsoft, 2026e).

The account-key connection is used only by the API to upload files and generate read-only SAS URLs. Confirm Azure is active from returned image URLs on `racedayst10441951.blob.core.windows.net`. A `127.0.0.1:10000` address means the API is using Azurite.

Azurite remains available for local development and regression tests; see [Part 2 storage setup](Part-2-Images.md#4-setup). The API tests create their own temporary containers.

## 4. Checks and evidence

On 19 November 2026, all 502 MVC checks passed locally, with none failed or skipped. Checks cover multipart fields, cookie isolation, roles, ownership, antiforgery, request/file limits, safe response URLs, replacement/removal requests and partial event/banner saves. A real HTTP test verifies separate browsers' upload cookies (Microsoft, 2026f).

Browser checks against the real API, SQL and Azurite passed for uploads, JPEG/PNG replacement, old-blob deletion, private signed reads, invalid files, size limits, storage failures, banner creation/editing, public display and confirmed removal. Account fields and official results were preserved. Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors.

The same checks passed against Azure on that date. Returned URLs used HTTPS on `racedayst10441951.blob.core.windows.net`, with read-only SAS permissions and a one-hour expiry. Signed reads returned 200; anonymous reads were denied. Replacement and removal made the old blob URLs return 404. SQL stored generated blob names. The API was started from the user's Windows session with its private Azure settings; no keys were copied into MVC or Git.

Screenshots use fictional sample accounts and simple test graphics. They are genuine browser captures without cursor overlays. Test pictures and banners were removed afterwards, and the isolated database was deleted. The original sample database was unchanged.

| Page | Evidence |
|---|---|
| Picture upload | [Desktop](evidence/part3-azure-picture-form.png) / [mobile](evidence/part3-azure-picture-form-mobile.png) |
| Saved profile picture | [Desktop](evidence/part3-azure-profile-picture.png) / [mobile](evidence/part3-azure-profile-picture-mobile.png) |
| Replacement and removal | [Replaced](evidence/part3-azure-picture-replaced.png) / [confirmation](evidence/part3-azure-picture-remove-confirmation.png) / [removed](evidence/part3-azure-picture-removed.png) |
| Banner during creation | [Create form](evidence/part3-azure-create-event-banner.png) |
| Saved event banner | [Desktop](evidence/part3-azure-event-banner.png) / [mobile](evidence/part3-azure-event-banner-mobile.png) |
| Public event banner | [Desktop](evidence/part3-azure-public-event-banner.png) / [mobile](evidence/part3-azure-public-event-banner-mobile.png) |
| Banner changes | [Replaced](evidence/part3-azure-banner-replaced.png) / [confirmation](evidence/part3-azure-banner-remove-confirmation.png) / [removed](evidence/part3-azure-banner-removed.png) |
| Invalid uploads | [Rejected content](evidence/part3-azure-image-rejected.png) / [size limit](evidence/part3-azure-image-size-rejected.png) |
| Storage failure | [Friendly error](evidence/part3-azure-storage-unavailable.png) |
| Verification | [Azure check results](evidence/part3-azure-image-checks.json) |

## References

Microsoft (2026a) 'Upload files in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026b) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026c) 'Upload a blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload (Accessed: 19 November 2026).

Microsoft (2026d) 'Create a service SAS for a container or blob with .NET', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/sas-service-create-dotnet (Accessed: 19 November 2026).

Microsoft (2026e) 'Safe storage of app secrets in development in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0 (Accessed: 19 November 2026).

Microsoft (2026f) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 19 November 2026).
