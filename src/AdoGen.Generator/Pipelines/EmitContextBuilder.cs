using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AdoGen.Generator.Emitters;
using AdoGen.Generator.Emitters.PostgreSql;
using AdoGen.Generator.Emitters.SqlServer;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Pipelines;

internal static class EmitContextBuilder
{
    private static readonly IIdentifierQuoter[] Quoters = 
        [PostgreSqlIdentifierQuoter.Instance, SqlServerIdentifierQuoter.Instance];
    
    public static EmitContext Build(ValidatedDiscoveryDto v) => BuildOne(v);

    private static EmitContext BuildOne(ValidatedDiscoveryDto v)
    {
        var (discovery, profile, _) = v;
        var dto = discovery.Dto;
        var provider = discovery.Provider;

        var quoter = Quoters.First(x => x.IsMatch(v));

        var dtoTypeName = dto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeKeyword = dto.IsRecord ? "record" : "class";
        var accessibility = dto.DeclaredAccessibility.ToString().ToLowerInvariant();
        var schemaQuoted = quoter.Quote(profile.Schema);
        var tableQuoted = quoter.Quote(profile.Table);
        var schemaTableQuoted = quoter.QuoteSchemaTable(profile.Schema, profile.Table);
        var factoryClassName = quoter.FactoryClassName(dto.Name);

        // Build ColumnInfo array from properties
        var columnsBuilder = ImmutableArray.CreateBuilder<ColumnInfo>(profile.DtoProperties.Length);
        
        for (var i = 0; i < profile.DtoProperties.Length; i++)
        {
            var p = profile.DtoProperties[i];
            var cfg = profile.ParamsByProperty[p.Name];
            var isKey = profile.Keys.Contains(p.Name);
            var isIdentity = profile.IdentityKeys.Contains(p.Name);
            var isNullable = p.IsNullableProperty(cfg);
            var defaultSql = p.ResolveDefaultSql(cfg, provider);
            var isReadOnly = cfg.IsReadOnly;
            var role = isIdentity ? ColumnRole.Identity : isKey ? ColumnRole.Key : ColumnRole.Plain;

            columnsBuilder.Add(new ColumnInfo(
                Name: p.Name,
                ColumnNameQuoted: quoter.Quote(cfg.ParameterName),
                ParameterName: cfg.ParameterName,
                SqlType: cfg.SqlTypeLiteral,
                PropertyType: cfg.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsNullable: isNullable,
                IsIdentity: isIdentity,
                IsKey: isKey,
                IsReadOnly: isReadOnly,
                DefaultSqlExpression: defaultSql,
                Role: role
            ));
        }

        var columns = columnsBuilder.ToImmutable();

        // Pre-compute subsets — avoid LINQ in emitters
        var keysBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var identitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var nonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var nonKeyNonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var writablesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var writableNonKeyNonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var bulkColumnsBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();

        for (var i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col.IsKey) keysBuilder.Add(col);
            if (col.IsIdentity) identitiesBuilder.Add(col);
            if (!col.IsIdentity) nonIdentitiesBuilder.Add(col);
            if (!col.IsKey && !col.IsIdentity) nonKeyNonIdentitiesBuilder.Add(col);
            if (!col.IsIdentity && !col.IsReadOnly) writablesBuilder.Add(col);
            if (!col.IsKey && !col.IsIdentity && !col.IsReadOnly) writableNonKeyNonIdentitiesBuilder.Add(col);
            // BulkColumns: keys always (for JOIN matching) + writable non-keys
            if (col.IsKey || !col.IsReadOnly) bulkColumnsBuilder.Add(col);
        }

        var keys = keysBuilder.ToImmutable();
        var identities = identitiesBuilder.ToImmutable();
        var nonIdentities = nonIdentitiesBuilder.ToImmutable();
        var nonKeyNonIdentities = nonKeyNonIdentitiesBuilder.ToImmutable();
        var writables = writablesBuilder.ToImmutable();
        var writableNonKeyNonIdentities = writableNonKeyNonIdentitiesBuilder.ToImmutable();
        var bulkColumns = bulkColumnsBuilder.ToImmutable();

        // WhereByKey: "[a] = @a AND [b] = @b" (spaces around = for readability)
        var whereByKey = BuildJoinedPredicate(keys, col => $"{col.ColumnNameQuoted} = @{col.ParameterName}");

        // JoinOn: "S.[k]=T.[k] AND ..."
        var joinOn = BuildJoinedPredicate(keys, col => $"S.{col.ColumnNameQuoted} = T.{col.ColumnNameQuoted}");

        return new EmitContext(
            Provider: provider,
            DtoTypeName: dtoTypeName,
            DtoSimpleName: dto.Name,
            Namespace: profile.Namespace,
            TypeKeyword: typeKeyword,
            Accessibility: accessibility,
            SchemaQuoted: schemaQuoted,
            TableQuoted: tableQuoted,
            SchemaTableQuoted: schemaTableQuoted,
            FactoryClassName: factoryClassName,
            Columns: columns,
            Keys: keys,
            Identities: identities,
            NonIdentities: nonIdentities,
            NonKeyNonIdentities: nonKeyNonIdentities,
            Writables: writables,
            WritableNonKeyNonIdentities: writableNonKeyNonIdentities,
            BulkColumns: bulkColumns,
            WhereByKey: whereByKey,
            JoinOn: joinOn,
            Quoter: quoter,
            Profile: profile
        );
    }

    private static string BuildJoinedPredicate(
        ImmutableArray<ColumnInfo> columns,
        Func<ColumnInfo, string> predicate)
    {
        if (columns.Length == 0) return string.Empty;
        if (columns.Length == 1) return predicate(columns[0]);

        var sb = new StringBuilder(capacity: columns.Length * 32);
        
        for (var i = 0; i < columns.Length; i++)
        {
            if (i > 0) sb.Append(" AND ");
            sb.Append(predicate(columns[i]));
        }
        
        return sb.ToString();
    }
}
