# EF Core And DevExpress Business Object Guidance

LocalGPT should treat Entity Framework entity generation as an architecture
decision, not just a class-writing task. The right entity shape depends on the
API/UI stack, whether DevExpress Web API/XAF/OData is involved, and whether the
database already contains data.

## Ask Before Generating Entities

When the user asks for an EF database or generated business objects, ask which
style they want when it is not already clear:

- DevExpress Web API/XAF/OData business objects with referential architecture,
  security system compatibility, and attribute-discoverable metadata.
- Plain ASP.NET Core EF backend with services/controllers/minimal APIs and no
  DevExpress Web API object exposure.
- Snapshot/audit style entities, field-aware change tracking, explicit backing
  fields, lazy loading proxies, eager-loading only, or no lazy loading.
- Delete behavior per relationship: Restrict, NoAction, Cascade, ClientSetNull,
  or soft-delete/audit tables.
- Migration style for existing data: nullable new columns, default values,
  backfill script, or multi-step migration.
- Naming rules for reverse-engineered schemas, including whether field names and
  property names may differ only by the first character casing.

## Attribute Style Versus ModelBuilder

Use attributes when the metadata must be visible to EF Core, OData,
DevExpress Web API/XAF, validation, Swagger/OpenAPI, or UI generation without
extra fluent configuration. Typical attribute-backed decisions include key
columns, foreign keys, inverse properties, display names, required/max length,
not mapped members, concurrency tokens, and DevExpress/XAF UI/security metadata.

Use `OnModelCreating`/ModelBuilder when the mapping is relationally complex,
cross-cutting, provider-specific, or would make attributes noisy. Good examples
are composite keys, alternate keys, indexes, owned types, precision/conversion,
global query filters, table splitting, many-to-many join entities, delete
behavior, and database-specific column types.

For DevExpress Web API/XAF/OData generation, prefer an attribute-discoverable
business object model plus a small, explicit ModelBuilder layer for the pieces
that attributes cannot express well. For plain EF backends, a cleaner POCO model
with fluent configuration may be enough and often keeps domain classes less
framework-heavy.

## Avoid Accidental Shadow Properties

Shadow properties are useful when intentionally modeled, but accidental shadow
foreign keys make generated code harder to review, debug, expose through OData,
and bind in DevExpress UI. Prefer explicit scalar foreign key properties plus
navigation properties:

```csharp
public Guid ChatId { get; set; }

[ForeignKey(nameof(ChatId))]
public TelegramChat Chat { get; set; } = null!;
```

Accidental shadow properties are usually caused by:

- Navigation properties without a matching FK scalar property.
- FK scalar names that do not match EF conventions or the configured
  `[ForeignKey]`/`HasForeignKey`.
- Duplicate or mismatched navigations without `[InverseProperty]`.
- Field/property casing or spelling drift, especially in reverse-engineered
  databases where only the first character casing is allowed to differ.
- Private backing fields that EF discovers differently than intended.
- Nullable reference type mismatches between FK nullability and navigation
  nullability.

Prevent them by using stable names, explicit FK properties, `[ForeignKey]`,
`[InverseProperty]`, and targeted ModelBuilder configuration. When reverse
engineering an existing database, compare generated migrations/model snapshots
for unexpected columns such as `SomethingId1`, `SomethingFK`, or shadow FK names.

## Naming Discipline

For reverse-engineered databases, naming is part of compatibility. If a schema
requires a field/property pair where only the first character changes case, do
not "clean it up" casually. A property such as `MessageId` should map to a field
or column whose relationship is obvious and deterministic. Renaming can break
OData metadata, DevExpress Web API exposure, model binding, migrations, and
human review.

If the user provides a reverse-engineered Telegram database or similar schema,
preserve exact table/column relationship semantics first. Only propose cosmetic
renames behind a user-approved migration plan.

## Nullable Columns In Existing Databases

When adding a new column to a populated database, nullable is often the correct
first migration even if future rows should always fill it. Existing rows do not
have the new data. Safer choices are:

- Add nullable column, deploy, backfill, then optionally make it required in a
  later migration.
- Add required column with a real default value only when the default is
  semantically correct for historical rows.
- Add a separate derived/read model if old data cannot honestly supply the
  value.

The council should explain this tradeoff instead of blindly generating
`required`/`NOT NULL` columns.

## DevExpress Web API/XAF Specific Guidance

When the user asks for a DevExpress Web API, OData, or XAF-compatible backend,
business objects should be designed for referential exposure:

- Explicit primary keys and foreign keys.
- Navigation properties with explicit inverse relationships.
- Attribute-visible metadata for validation, display, security, and generated UI.
- Stable public properties for OData and DevExpress model discovery.
- Avoidance of accidental shadow properties and ambiguous backing fields.
- Clear delete behavior and security implications for each relationship.

When the user asks for a plain EF backend without DevExpress Web API/XAF
exposure, do not force this heavier shape. Use ordinary EF services and DTOs when
that gives a simpler and safer API.

## Helpful Sources

- DevExpress XAF Data Annotation Attributes:
  https://docs.devexpress.com/eXpressAppFramework/112701/business-model-design-orm/data-annotations-in-data-model
- DevExpress Backend Web API Service:
  https://docs.devexpress.com/eXpressAppFramework/403394/backend-web-api-service
- DevExpress OData EDM customization:
  https://docs.devexpress.com/eXpressAppFramework/403719/backend-web-api-service/use-odata-to-send-requests/customize-odata-options
- Microsoft EF Core shadow properties:
  https://learn.microsoft.com/ef/core/modeling/shadow-properties
- Microsoft EF Core relationship mapping attributes:
  https://learn.microsoft.com/ef/core/modeling/relationships/mapping-attributes
- Microsoft EF Core entity properties:
  https://learn.microsoft.com/ef/core/modeling/entity-properties
