using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// Fixed sample data is copied from the corrected docs/RaceDay.sql.
// Model-managed seed data: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding#model-managed-data
// Accessed: 18 October 2026.
// Keep fixed dates and existing password hashes so migrations remain repeatable.
internal static class SeedData
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                RoleId = 1,
                RoleName = "Organiser"
            },
            new Role
            {
                RoleId = 2,
                RoleName = "Participant"
            });

        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                RoleId = 1,
                FirstName = "Thabo",
                LastName = "Ndlovu",
                Email = "thabo.ndlovu@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEMJEo34Vex/Es3ymj/KGDAsh0XTg/maiNgOcLa2boUyPUM5+RCqT/0h++cgbZz9QKQ==",
                PhoneNumber = "0825550101",
                DateOfBirth = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                UserId = 2,
                RoleId = 1,
                FirstName = "Ayesha",
                LastName = "Khan",
                Email = "ayesha.khan@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAENQbJEgO1gVHjHJ5qFByozUY2cbU8ylUaFYisC4nx4Hg/xMev27k+Ge6phAOBOsxCA==",
                PhoneNumber = "0835550102",
                DateOfBirth = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 19, 12, 5, 0, DateTimeKind.Utc)
            },
            new User
            {
                UserId = 3,
                RoleId = 2,
                FirstName = "Naledi",
                LastName = "Mokoena",
                Email = "naledi.mokoena@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEDGx3Kf8pdpEdcvJgX7WsWvSff/SlnpYHBOzinfgqgYepnz19uEvaNNth2Tp67JSKQ==",
                PhoneNumber = "0845550103",
                DateOfBirth = new DateOnly(1994, 6, 12),
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 19, 12, 10, 0, DateTimeKind.Utc)
            },
            new User
            {
                UserId = 4,
                RoleId = 2,
                FirstName = "Sipho",
                LastName = "Dlamini",
                Email = "sipho.dlamini@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEGKZ3ZVEHVCoL6bra3uVYqs9oPOEklCO8xFdN2QUbsquTGekE1T4L64zlVfYua6tuw==",
                PhoneNumber = "0715550104",
                DateOfBirth = new DateOnly(1989, 11, 3),
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 19, 12, 15, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                EventId = 1,
                OrganiserUserId = 1,
                EventName = "Durban Sunrise 10K",
                EventType = "Running",
                Description = "A coastal road race with five and ten kilometre categories.",
                DistanceKm = 10.00m,
                EventDateTime = new DateTime(2026, 8, 16, 4, 30, 0, DateTimeKind.Utc),
                EntryClosingDateTime = new DateTime(2026, 8, 9, 21, 59, 59, DateTimeKind.Utc),
                VenueName = "Moses Mabhida Stadium",
                AddressLine1 = "44 Isaiah Ntshangase Road",
                City = "Durban",
                Province = "KwaZulu-Natal",
                Latitude = -29.829000m,
                Longitude = 31.030300m,
                Status = "Completed",
                CreatedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new Event
            {
                EventId = 2,
                OrganiserUserId = 2,
                EventName = "Cape Peninsula Cycle Challenge",
                EventType = "Cycling",
                Description = "A supported road cycling event with forty and eighty kilometre routes.",
                DistanceKm = 80.00m,
                EventDateTime = new DateTime(2027, 3, 14, 4, 0, 0, DateTimeKind.Utc),
                EntryClosingDateTime = new DateTime(2027, 3, 1, 21, 59, 59, DateTimeKind.Utc),
                VenueName = "Green Point Urban Park",
                AddressLine1 = "1 Fritz Sonnenberg Road",
                City = "Cape Town",
                Province = "Western Cape",
                Latitude = -33.905800m,
                Longitude = 18.411900m,
                Status = "Open",
                CreatedAt = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc)
            },
            new Event
            {
                EventId = 3,
                OrganiserUserId = 1,
                EventName = "Soweto Heritage Walk",
                EventType = "Walking",
                Description = "A community walk visiting important heritage locations in Soweto.",
                DistanceKm = 10.00m,
                EventDateTime = new DateTime(2027, 4, 24, 5, 30, 0, DateTimeKind.Utc),
                EntryClosingDateTime = new DateTime(2027, 4, 17, 21, 59, 59, DateTimeKind.Utc),
                VenueName = "Walter Sisulu Square",
                AddressLine1 = "1 Klipspruit Valley Road",
                City = "Soweto",
                Province = "Gauteng",
                Latitude = -26.278400m,
                Longitude = 27.858500m,
                Status = "Open",
                CreatedAt = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<EventCategory>().HasData(
            new EventCategory
            {
                EventCategoryId = 1,
                EventId = 1,
                CategoryName = "5 km Run",
                DistanceKm = 5.00m,
                MinimumAge = 10,
                MaximumAge = null,
                EntryFee = 120.00m,
                Capacity = 1800,
                IsAvailable = false,
                CreatedAt = new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc)
            },
            new EventCategory
            {
                EventCategoryId = 2,
                EventId = 1,
                CategoryName = "10 km Run",
                DistanceKm = 10.00m,
                MinimumAge = 15,
                MaximumAge = null,
                EntryFee = 180.00m,
                Capacity = 2500,
                IsAvailable = false,
                CreatedAt = new DateTime(2026, 5, 2, 8, 5, 0, DateTimeKind.Utc)
            },
            new EventCategory
            {
                EventCategoryId = 3,
                EventId = 2,
                CategoryName = "40 km Cycle",
                DistanceKm = 40.00m,
                MinimumAge = 16,
                MaximumAge = null,
                EntryFee = 350.00m,
                Capacity = 1500,
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc)
            },
            new EventCategory
            {
                EventCategoryId = 4,
                EventId = 2,
                CategoryName = "80 km Cycle",
                DistanceKm = 80.00m,
                MinimumAge = 18,
                MaximumAge = null,
                EntryFee = 550.00m,
                Capacity = 1000,
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 9, 2, 9, 5, 0, DateTimeKind.Utc)
            },
            new EventCategory
            {
                EventCategoryId = 5,
                EventId = 3,
                CategoryName = "Under 20 5 km Walk",
                DistanceKm = 5.00m,
                MinimumAge = null,
                MaximumAge = 19,
                EntryFee = 80.00m,
                Capacity = 2000,
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc)
            },
            new EventCategory
            {
                EventCategoryId = 6,
                EventId = 3,
                CategoryName = "10 km Walk",
                DistanceKm = 10.00m,
                MinimumAge = 12,
                MaximumAge = null,
                EntryFee = 120.00m,
                Capacity = 1200,
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 9, 11, 10, 5, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<EventRoute>().HasData(
            new EventRoute
            {
                RouteId = 1,
                EventCategoryId = 1,
                RouteName = "Stadium 5 km Loop",
                StartLocation = "Moses Mabhida Stadium south entrance",
                FinishLocation = "Moses Mabhida Stadium south entrance",
                RouteDescription = "A flat out-and-back route along Masabalala Yengwa Avenue.",
                RouteMapUrl = null,
                ElevationGainMetres = 35,
                CreatedAt = new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc)
            },
            new EventRoute
            {
                RouteId = 2,
                EventCategoryId = 2,
                RouteName = "Stadium Coastal Loop",
                StartLocation = "Moses Mabhida Stadium south entrance",
                FinishLocation = "Moses Mabhida Stadium south entrance",
                RouteDescription = "A coastal loop passing the beachfront before returning to the stadium.",
                RouteMapUrl = null,
                ElevationGainMetres = 85,
                CreatedAt = new DateTime(2026, 5, 3, 8, 5, 0, DateTimeKind.Utc)
            },
            new EventRoute
            {
                RouteId = 3,
                EventCategoryId = 3,
                RouteName = "Peninsula Short Route",
                StartLocation = "Green Point Urban Park",
                FinishLocation = "Green Point Urban Park",
                RouteDescription = "A forty kilometre road route through the Atlantic Seaboard.",
                RouteMapUrl = null,
                ElevationGainMetres = 420,
                CreatedAt = new DateTime(2026, 9, 3, 9, 0, 0, DateTimeKind.Utc)
            },
            new EventRoute
            {
                RouteId = 4,
                EventCategoryId = 4,
                RouteName = "Peninsula Long Route",
                StartLocation = "Green Point Urban Park",
                FinishLocation = "Green Point Urban Park",
                RouteDescription = "An eighty kilometre endurance route through the southern peninsula.",
                RouteMapUrl = null,
                ElevationGainMetres = 980,
                CreatedAt = new DateTime(2026, 9, 3, 9, 5, 0, DateTimeKind.Utc)
            },
            new EventRoute
            {
                RouteId = 5,
                EventCategoryId = 5,
                RouteName = "Kliptown Heritage Route",
                StartLocation = "Walter Sisulu Square",
                FinishLocation = "Walter Sisulu Square",
                RouteDescription = "A five kilometre community route through Kliptown.",
                RouteMapUrl = null,
                ElevationGainMetres = 45,
                CreatedAt = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc)
            },
            new EventRoute
            {
                RouteId = 6,
                EventCategoryId = 6,
                RouteName = "Soweto Heritage Route",
                StartLocation = "Walter Sisulu Square",
                FinishLocation = "Walter Sisulu Square",
                RouteDescription = "A ten kilometre walking route connecting several heritage locations.",
                RouteMapUrl = null,
                ElevationGainMetres = 110,
                CreatedAt = new DateTime(2026, 9, 12, 10, 5, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<Enrolment>().HasData(
            new Enrolment
            {
                EnrolmentId = 1,
                ParticipantUserId = 3,
                EventId = 1,
                EventCategoryId = 2,
                EnrolmentDate = new DateTime(2026, 6, 20, 9, 15, 0, DateTimeKind.Utc),
                Status = "Confirmed",
                BibNumber = "D1001",
                UpdatedAt = null
            },
            new Enrolment
            {
                EnrolmentId = 2,
                ParticipantUserId = 4,
                EventId = 1,
                EventCategoryId = 1,
                EnrolmentDate = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc),
                Status = "Confirmed",
                BibNumber = "D0501",
                UpdatedAt = null
            },
            new Enrolment
            {
                EnrolmentId = 3,
                ParticipantUserId = 3,
                EventId = 2,
                EventCategoryId = 3,
                EnrolmentDate = new DateTime(2026, 9, 18, 11, 20, 0, DateTimeKind.Utc),
                Status = "Confirmed",
                BibNumber = "C4001",
                UpdatedAt = null
            },
            new Enrolment
            {
                EnrolmentId = 4,
                ParticipantUserId = 4,
                EventId = 3,
                EventCategoryId = 6,
                EnrolmentDate = new DateTime(2026, 9, 16, 15, 45, 0, DateTimeKind.Utc),
                Status = "Cancelled",
                BibNumber = null,
                UpdatedAt = new DateTime(2026, 9, 18, 8, 10, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<Result>().HasData(
            new Result
            {
                ResultId = 1,
                EnrolmentId = 1,
                RecordedByUserId = 1,
                ResultStatus = "Completed",
                FinishTimeSeconds = 2874,
                OverallPosition = 42,
                CategoryPosition = 10,
                Notes = null,
                RecordedAt = new DateTime(2026, 8, 16, 6, 15, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new Result
            {
                ResultId = 2,
                EnrolmentId = 2,
                RecordedByUserId = 1,
                ResultStatus = "DidNotFinish",
                FinishTimeSeconds = null,
                OverallPosition = null,
                CategoryPosition = null,
                Notes = "Participant withdrew at the three kilometre water point.",
                RecordedAt = new DateTime(2026, 8, 16, 6, 20, 0, DateTimeKind.Utc),
                UpdatedAt = null
            });
    }
}
