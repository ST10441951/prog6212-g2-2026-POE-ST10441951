namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in Roles.</summary>
public sealed class Role
{
    /// <summary>Fixed identifier for a Role.</summary>
    public byte RoleId { get; set; }

    /// <summary>Role name, limited to Organiser or Participant.</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Users assigned this role.</summary>
    public ICollection<User> Users { get; set; } = [];
}
