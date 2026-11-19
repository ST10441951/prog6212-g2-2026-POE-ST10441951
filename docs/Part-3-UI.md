# RaceDay Part 3 - Interface

## 1. Layout

The website keeps the Event finder design: blue accents, white cards, a light background and one shared header/footer. Bootstrap is served locally. Both roles use the same form fields, buttons and validation styles (Bootstrap, 2026).

The header shows the current section with colour and an underline. Organisers reach categories, enrolments and results from My events. Participants use My entries, My results and My profile. A keyboard skip link moves straight to the page content (MDN, 2026a).

Uploaded banners also appear in the event list. MVC uses the image URL returned by the API, checks that it is an absolute HTTP/HTTPS address without embedded credentials, and sets alternative text and lazy loading. Events without a usable banner keep their date, title and details link (MDN, 2026b).

## 2. Status and forms

| Event status | Colour |
|---|---|
| Draft | Grey |
| Open | Green |
| Closed | Amber |
| Completed | Blue |
| Cancelled | Red |

Entry badges use green for Confirmed and red for Cancelled. Status text stays visible alongside the colour; no Pending status is added because the API does not use it.

Form fields have a consistent minimum height of 44 pixels. Buttons, spacing and error messages match across pages. Long event names wrap within the heading. Invalid result times show a summary and an inline error while the saved result stays unchanged.

## 3. Checks and evidence

On 19 November 2026, 61 pages passed checks at 320, 390, 768, 1024 and 1280 pixels locally and in Docker. These covered guest pages, both roles, forms, private details, empty lists, rejected filters and access-denied pages. There was no horizontal overflow or JavaScript error. Visible fields had labels, images had alternative text and each page had one main heading.

Keyboard checks reached the skip link first and moved focus to the content for guests and both roles. A 120-character event name also stayed within the page at all five widths. All 512 MVC tests passed locally, including safe, missing and rejected event-banner URLs.

Screenshots use fictional sample accounts and an uploaded test banner. They contain no cursor overlays. The isolated database and local image container were removed after checking. The [check results](evidence/part3-ui-checks.json) record both local and container verification.

| Page | Evidence |
|---|---|
| Event finder with banner | [Desktop](evidence/part3-ui-guest-home.png) / [mobile](evidence/part3-ui-guest-home-mobile.png) |
| Organiser dashboard | [Desktop](evidence/part3-ui-organiser-organiser.png) / [mobile](evidence/part3-ui-organiser-organiser-mobile.png) |
| Create event form | [Desktop](evidence/part3-ui-organiser-organiser-events-create.png) / [mobile](evidence/part3-ui-organiser-organiser-events-create-mobile.png) |
| My entries | [Desktop](evidence/part3-ui-participant-entries.png) / [mobile](evidence/part3-ui-participant-entries-mobile.png) |
| Sign in | [Mobile](evidence/part3-ui-guest-account-login-mobile.png) |
| Rejected result time | [Mobile validation](evidence/part3-ui-result-validation-mobile.png) |

## References

Bootstrap (2026) 'Forms', *Bootstrap v5.3*. Available at: https://getbootstrap.com/docs/5.3/forms/overview/ (Accessed: 19 November 2026).

MDN (2026a) 'ARIA: aria-current attribute', *MDN Web Docs*. Available at: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-current (Accessed: 19 November 2026).

MDN (2026b) '\<img\>: The Image Embed element', *MDN Web Docs*. Available at: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/img (Accessed: 19 November 2026).
