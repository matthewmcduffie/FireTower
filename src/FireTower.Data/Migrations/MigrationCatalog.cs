namespace FireTower.Data.Migrations;

/// <summary>
/// Every schema migration FireTower has ever shipped, in order. Add new entries here;
/// never edit a migration that has already been released.
/// </summary>
public static class MigrationCatalog
{
    public static readonly IReadOnlyList<Migration> All = new[]
    {
        new Migration(1, "Initial schema", InitialSchema.Sql),
    };
}
