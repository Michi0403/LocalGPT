namespace LocalGPT.BusinessObjects;

/// <summary>
/// Describes one obsolete SQLite column that an EF migration replaced but cannot drop, because the SQLite migration SQL
/// generator rejects <c>DropColumnOperation</c>. The migration compatibility service drops it after migration, and only
/// after checking the real database: the column still exists, the replacing migration is recorded, the replacement
/// columns exist, nothing in the schema still references the column, and the backfill has run.
/// </summary>
/// <param name="TableName">Table that still carries the obsolete column.</param>
/// <param name="ColumnName">Obsolete column to drop.</param>
/// <param name="RequiredMigrationId">EF migration that moved the column's meaning into the replacement columns; it must be recorded as applied.</param>
/// <param name="ReplacementColumns">Columns that must exist before the obsolete column may be dropped.</param>
/// <param name="BackfillSql">Idempotent statement that maps rows the migration did not map yet; it runs in the same transaction as the drop.</param>
internal sealed record SqliteColumnRetirement(
    string TableName,
    string ColumnName,
    string RequiredMigrationId,
    string[] ReplacementColumns,
    string BackfillSql);
