namespace FireTower.Data.Migrations;

/// <summary>
/// A single versioned, repeatable schema change applied in order against
/// <c>PRAGMA user_version</c>, per the Migrations requirements in database.md.
/// </summary>
public sealed record Migration(int Version, string Description, string Sql);
