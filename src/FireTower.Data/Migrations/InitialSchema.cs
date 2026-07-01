namespace FireTower.Data.Migrations;

/// <summary>
/// Schema for migration version 1: the tables described in database.md.
/// Every statement uses IF NOT EXISTS so this migration is safe to apply against
/// a database that already has some or all tables from a pre-versioned dev run.
/// </summary>
internal static class InitialSchema
{
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS VirtualMachines (
            Id TEXT PRIMARY KEY,
            ExternalId TEXT NOT NULL,
            Name TEXT NOT NULL,
            ProviderId TEXT NOT NULL,
            Enabled INTEGER NOT NULL,
            HealthProfileId TEXT NOT NULL,
            RecoveryProfileId TEXT NOT NULL,
            Tags TEXT NOT NULL DEFAULT '',
            PowerState TEXT NOT NULL,
            Health TEXT NOT NULL,
            RecoveryState TEXT NOT NULL,
            RestartCount INTEGER NOT NULL DEFAULT 0,
            LastHealthCheck TEXT NULL,
            LastRestart TEXT NULL,
            DateCreated TEXT NOT NULL,
            DateModified TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS HealthHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            VirtualMachineId TEXT NOT NULL REFERENCES VirtualMachines(Id),
            PreviousState TEXT NOT NULL,
            NewState TEXT NOT NULL,
            CheckResultsJson TEXT NOT NULL,
            Timestamp TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_healthhistory_vm_timestamp ON HealthHistory(VirtualMachineId, Timestamp);

        CREATE TABLE IF NOT EXISTS RestartHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            VirtualMachineId TEXT NOT NULL REFERENCES VirtualMachines(Id),
            Action TEXT NOT NULL,
            Success INTEGER NOT NULL,
            FailureCategory TEXT NOT NULL,
            FailureReason TEXT NULL,
            DurationMs INTEGER NOT NULL,
            Timestamp TEXT NOT NULL,
            CorrelationId TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_restarthistory_vm_timestamp ON RestartHistory(VirtualMachineId, Timestamp);
        CREATE INDEX IF NOT EXISTS idx_restarthistory_correlation ON RestartHistory(CorrelationId);

        CREATE TABLE IF NOT EXISTS Events (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Timestamp TEXT NOT NULL,
            Category TEXT NOT NULL,
            Message TEXT NOT NULL,
            VirtualMachineId TEXT NULL,
            CorrelationId TEXT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_events_timestamp ON Events(Timestamp);

        CREATE TABLE IF NOT EXISTS StatisticsSnapshots (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Timestamp TEXT NOT NULL,
            TotalVmCount INTEGER NOT NULL,
            HealthyCount INTEGER NOT NULL,
            WarningCount INTEGER NOT NULL,
            CriticalCount INTEGER NOT NULL,
            RestartCount INTEGER NOT NULL,
            AverageRestartDurationSeconds REAL NOT NULL,
            AverageHealthCheckDurationMs REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_statistics_timestamp ON StatisticsSnapshots(Timestamp);
        """;
}
