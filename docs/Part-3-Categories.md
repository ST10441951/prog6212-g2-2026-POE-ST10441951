# RaceDay Part 3 - Categories and routes

## 1. Pages

All pages begin with `/organiser/events/{eventId}/categories`. Only the owning Organiser can use them. Event details contain Manage categories and routes; My events stays selected in the menu (Microsoft, 2026a).

| MVC page | API request |
|---|---|
| Category list | GET `/api/organiser/events/{eventId}/categories` |
| Add category | POST `/api/events/{eventId}/categories` |
| Edit/delete category | PUT / DELETE `/api/events/{eventId}/categories/{categoryId}` |
| Route details and forms | Private event details with the category's current route |
| Add/edit/remove route | POST / PUT / DELETE `/api/events/{eventId}/categories/{categoryId}/route` |

Private event membership is checked before forms or writes are allowed. Category IDs must belong to that event. The API repeats ownership and relationship checks on each write. Posted IDs and owners are not sent as editable fields (Microsoft, 2026a; 2026b).

## 2. Categories

The list includes unavailable categories, distance, age limits, rand fees, capacity and route name. Capacity is a maximum entry count, not a count of remaining places. Age limits and capacity are optional; blank capacity means no limit.

Add/edit sends only name, distance, minimum/maximum age, fee, capacity and availability. The API validates age order, precision, names and capacity. Saved entries protect distance, age limits and fee. An Open event must keep an available category. Rejected forms retain the input and show the field error or conflict reason (Microsoft, 2026b; 2026c).

Deletion opens a read-only confirmation. Keep category makes no change. The API blocks deletion when entries exist or Open-event readiness would be lost. An unused category is deleted with its optional route; the parent event remains.

## 3. Routes

Each category can have one route with name, start, finish, instructions, optional map URL and elevation. Add/edit sends only these six fields. Blank optional values clear them. The API accepts only absolute HTTP/HTTPS map links without credentials; MVC also checks saved URLs before rendering links.

Route corrections keep the route ID and category link. Removal requires confirmation and deletes only the route. Categories, entries and results remain. A removed route can be added again later. All forms require antiforgery tokens. Service failures show a friendly 503; uncertain writes advise checking saved data before retrying (Microsoft, 2026c; 2026d).

## 4. Checks and evidence

On 19 November 2026, all 518 MVC checks passed locally, with none failed or skipped. Checks cover roles, ownership, allowed fields, invalid category links, form errors, safe maps, response validation, confirmation, ended sessions and uncertain writes (Microsoft, 2026e).

Browser checks used an isolated database. Category and route saves matched SQL, including cleared optional values. Invalid ages, duplicate names, unsafe links, reduced capacity, last-available changes and protected-history changes were rejected. Other users were blocked. Unused category deletion also removed its route; route correction/removal preserved result-bearing history. Confirmation GETs and Keep links changed no data.

Pages passed at 320, 390, 768 and 1280 pixels without horizontal overflow or JavaScript errors. Screenshots are genuine browser captures without cursor overlays. The temporary database was removed afterwards; original sample data was unchanged.

| Page | Evidence |
|---|---|
| Categories | [Desktop](evidence/part3-categories.png) / [mobile](evidence/part3-categories-mobile.png) |
| Add category | [Desktop](evidence/part3-category-create.png) / [mobile](evidence/part3-category-create-mobile.png) |
| Age validation | [Rejected ages](evidence/part3-category-validation.png) |
| Availability/capacity | [Readiness](evidence/part3-category-readiness-rejected.png) / [capacity](evidence/part3-category-capacity-rejected.png) |
| Category deletion | [Confirmation](evidence/part3-category-delete-confirmation.png) / [history restriction](evidence/part3-category-history-rejected.png) / [deleted](evidence/part3-category-deleted.png) |
| Add route | [Desktop](evidence/part3-route-create.png) / [mobile](evidence/part3-route-create-mobile.png) |
| Saved route | [Desktop](evidence/part3-route-details.png) / [mobile](evidence/part3-route-details-mobile.png) |
| Edit/remove route | [Edit](evidence/part3-route-edit.png) / [confirmation](evidence/part3-route-delete-confirmation.png) / [removed](evidence/part3-route-removed.png) |

## References

Microsoft (2026a) 'Role-based authorization in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026b) 'Model Binding in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026c) 'Model validation in ASP.NET Core MVC and Razor Pages', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026d) 'Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0 (Accessed: 18 November 2026).

Microsoft (2026e) 'Integration tests in ASP.NET Core', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0 (Accessed: 18 November 2026).
