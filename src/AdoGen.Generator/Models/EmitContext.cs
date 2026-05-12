using System.Collections.Immutable;
using AdoGen.Generator.Emitters;

namespace AdoGen.Generator.Models;

/// <summary>
/// Per-DTO precomputed context produced once after validation and shared across all emitters.
/// Must be a record to satisfy Roslyn incremental pipeline value-equality requirements.
/// </summary>
internal sealed record EmitContext(
    SqlProviderKind Provider,
    string DtoTypeName,             // fully qualified
    string DtoSimpleName,           // short name
    string Namespace,
    string TypeKeyword,             // "record" | "class"
    string Accessibility,           // "public" | "internal"
    string SchemaQuoted,
    string TableQuoted,
    string SchemaTableQuoted,       // [dbo].[User] or "public"."user"
    string FactoryClassName,        // {Dto}Sql or {Dto}Npgsql
    ImmutableArray<ColumnInfo> Columns,
    ImmutableArray<ColumnInfo> Keys,
    ImmutableArray<ColumnInfo> Identities,
    ImmutableArray<ColumnInfo> NonIdentities,
    ImmutableArray<ColumnInfo> NonKeyNonIdentities,
    ImmutableArray<ColumnInfo> Writables,              // NonIdentities excluding ReadOnly (for INSERT)
    ImmutableArray<ColumnInfo> WritableNonKeyNonIdentities, // NonKey+NonIdentity excluding ReadOnly (for UPDATE SET)
    ImmutableArray<ColumnInfo> BulkColumns,            // Keys + writable non-keys (for bulk temp-table / COPY)
    string WhereByKey,                       // "[a]=@a AND [b]=@b"
    string JoinOn,                           // "S.[k]=T.[k] AND ..."
    IIdentifierQuoter Quoter,
    ProfileInfo Profile,                     // back-reference for edge cases
    ColumnInfo? ConcurrencyToken             // null if no concurrency token configured
)
{
    public bool IsIdentity(string columnName) => Profile.IdentityKeys.Contains(columnName);
    public bool IsKey(string columnName) => Profile.Keys.Contains(columnName);
}

