using AdoGen.Generator.Models;

namespace AdoGen.Generator.Extensions;

internal static class ParamConfigExtensions
{
    /// <summary>
    /// For DTOs with a validated profile (ISqlDomainModel/ISqlBulkModel/etc.) this is always true —
    /// StringValidator*/DecimalValidator*/BinaryValidator* already enforce Size/Precision/Scale as a
    /// DDL-correctness requirement (AG004/AG005/AG006) independent of anything checked here.
    ///
    /// For mapper-only DTOs with no profile (see ProfileInfoCollector.ResolveMapperOnly), every
    /// property gets a convention default DbType but no Size/Precision/Scale. Rather than mirroring
    /// the DDL mandatory-config set, this method gates factory emission specifically on whether the
    /// missing metadata can actually cause a WHERE-clause query-performance regression when the
    /// caller binds the generated parameter against a hand-written query:
    ///
    /// - SQL Server: an unset Size/Precision/Scale is inferred per-value by ADO.NET (same mechanism
    ///   as the banned AddWithValue), so differently-shaped bound values (a 5-char vs 50-char string,
    ///   5.00m vs 5.1234m) get typed differently on every call, producing a distinct cached execution
    ///   plan per shape — plan-cache bloat and recompiles. String DbType additionally encodes
    ///   Unicode-ness (NVarChar vs VarChar); a mismatch against the real column wraps the column in
    ///   an implicit CONVERT and defeats index seeks. Both checks stay strict for SQL Server.
    /// - PostgreSQL: numeric is sent over the wire in a self-describing, arbitrary-precision format —
    ///   Precision/Scale are not part of the bind type signature, so they cannot cause plan-cache
    ///   bloat or affect index usage. varchar/text/char are binary-coercible, so a type-family
    ///   mismatch doesn't defeat index usage either, and Npgsql doesn't encode length into the wire
    ///   type the way SQL Server does, so there's no per-length plan variance. Only DbType being
    ///   present matters for Postgres strings/decimals; Size/Precision/Scale are irrelevant to
    ///   performance and no longer block factory emission. bytea has no length-modifier type variant
    ///   at all, so it was already exempt from a Size requirement.
    ///
    /// Map() itself is unaffected by any of this and is always emitted.
    /// </summary>
    public static bool CanEmitParameterFactory(this ParamConfig cfg, SqlProviderKind provider)
    {
        if (cfg.PropertyType.IsString)
        {
            return provider switch
            {
                SqlProviderKind.SqlServer => cfg.DbType is not null && cfg.Size is not null,
                SqlProviderKind.PostgreSql => cfg.DbType is not null,
                _ => true
            };
        }

        if (cfg.PropertyType.IsDecimal)
        {
            return provider switch
            {
                SqlProviderKind.SqlServer => cfg.DbType?.EnumMember == "Decimal" && cfg.Precision is not null && cfg.Scale is not null,
                SqlProviderKind.PostgreSql => cfg.DbType?.EnumMember == "Numeric",
                _ => true
            };
        }

        if (cfg.PropertyType.IsByteArray)
        {
            return provider switch
            {
                SqlProviderKind.SqlServer => cfg.DbType?.EnumMember == "VarBinary" && cfg.Size is not null,
                SqlProviderKind.PostgreSql => cfg.DbType?.EnumMember == "Bytea",
                _ => true
            };
        }

        return true;
    }
}
