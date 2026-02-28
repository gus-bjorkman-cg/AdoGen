using System;
using System.Data;
using Microsoft.CodeAnalysis;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Extensions;

internal static class RoslynSymbolExtensions
{
    public static readonly SymbolDisplayFormat GetterKeyFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    
    extension(ITypeSymbol t)
    {
        public bool IsString() => t.SpecialType == SpecialType.System_String;
        public bool IsDecimal() => t.SpecialType == SpecialType.System_Decimal;
        public bool IsByteArray() => t is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };
        public string GetGetterKey() => t.ToDisplayString(GetterKeyFormat);
        
        public bool IsGuidType()
        {
            // unwrap Nullable<T>
            if (t is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nt)
                t = nt.TypeArguments[0];

            return t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Guid";
        }
        
        public (ITypeSymbol Underlying, bool IsNullable) UnwrapNullable() =>
            t is INamedTypeSymbol 
                { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } named
                ? (named.TypeArguments[0], true)
                : (t, false);

        public DbTypeRef MapDefaultSqlDbType()
        {
            var (underlying, _) = t.UnwrapNullable();

            if (underlying.TypeKind == TypeKind.Enum && underlying is INamedTypeSymbol enumType)
            {
                var dbt = enumType.EnumUnderlyingType?.SpecialType switch
                {
                    SpecialType.System_Byte => SqlDbType.TinyInt,
                    SpecialType.System_SByte => SqlDbType.SmallInt,
                    SpecialType.System_Int16 => SqlDbType.SmallInt,
                    SpecialType.System_UInt16 => SqlDbType.Int,
                    SpecialType.System_Int32 => SqlDbType.Int,
                    SpecialType.System_UInt32 => SqlDbType.BigInt,
                    SpecialType.System_Int64 => SqlDbType.BigInt,
                    SpecialType.System_UInt64 => SqlDbType.Decimal,
                    _ => SqlDbType.Variant
                };

                return DbTypeRef.SqlServer(dbt.ToString());
            }

            var mapped = underlying.SpecialType switch
            {
                SpecialType.System_Boolean => SqlDbType.Bit,
                SpecialType.System_Byte => SqlDbType.TinyInt,
                SpecialType.System_Int16 => SqlDbType.SmallInt,
                SpecialType.System_Int32 => SqlDbType.Int,
                SpecialType.System_Int64 => SqlDbType.BigInt,
                SpecialType.System_Single => SqlDbType.Real,
                SpecialType.System_Double => SqlDbType.Float,
                SpecialType.System_Decimal => SqlDbType.Decimal,
                SpecialType.System_String => SqlDbType.NVarChar,
                _ => underlying.ToDisplayString(GetterKeyFormat) switch
                {
                    "global::System.Guid" => SqlDbType.UniqueIdentifier,
                    "global::System.DateTime" => SqlDbType.DateTime2,
                    "global::System.DateTimeOffset" => SqlDbType.DateTimeOffset,
                    "global::System.DateOnly" => SqlDbType.Date,
                    "global::System.TimeOnly" => SqlDbType.Time,
                    "global::System.Byte[]" => SqlDbType.VarBinary,
                    _ => SqlDbType.Variant
                }
            };

            return DbTypeRef.SqlServer(mapped.ToString());
        }

        public DbTypeRef MapDefaultNpgsqlDbType()
        {
            var (underlying, _) = t.UnwrapNullable();

            if (underlying.TypeKind == TypeKind.Enum && underlying is INamedTypeSymbol enumType)
            {
                var npg = enumType.EnumUnderlyingType?.SpecialType switch
                {
                    SpecialType.System_Byte => "Smallint",
                    SpecialType.System_SByte => "Smallint",
                    SpecialType.System_Int16 => "Smallint",
                    SpecialType.System_UInt16 => "Integer",
                    SpecialType.System_Int32 => "Integer",
                    SpecialType.System_UInt32 => "Bigint",
                    SpecialType.System_Int64 => "Bigint",
                    SpecialType.System_UInt64 => "Numeric",
                    _ => "Integer"
                };
                return DbTypeRef.PostgreSql(npg);
            }

            var mapped = underlying.SpecialType switch
            {
                SpecialType.System_Boolean => "Boolean",
                SpecialType.System_Byte => "Smallint",
                SpecialType.System_Int16 => "Smallint",
                SpecialType.System_Int32 => "Integer",
                SpecialType.System_Int64 => "Bigint",
                SpecialType.System_Single => "Real",
                SpecialType.System_Double => "Double",
                SpecialType.System_Decimal => "Numeric",
                SpecialType.System_String => "Text",
                SpecialType.System_Char => "Char",
                _ => underlying.ToDisplayString(GetterKeyFormat) switch
                {
                    "global::System.Guid" => "Uuid",
                    "global::System.DateTime" => "Timestamp",
                    "global::System.DateTimeOffset" => "TimestampTz",
                    "global::System.DateOnly" => "Date",
                    "global::System.TimeOnly" => "Time",
                    "global::System.Byte[]" => "Bytea",
                    _ => "Unknown"
                }
            };

            return DbTypeRef.PostgreSql(mapped);
        }
    }

    extension(IPropertySymbol prop)
    {
        public bool IsNullableProperty(ParamConfig cfg)
        {
            if (cfg.IsNullable is { } forced) return forced;

            var t = prop.Type;

            if (t.IsReferenceType)
            {
                // Follow C# nullable annotations: string? -> NULL, string -> NOT NULL
                return prop.NullableAnnotation == NullableAnnotation.Annotated;
            }

            // value types: Nullable<T> -> NULL
            if (t is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }) return true;

            return false;
        }

        
        public string? ResolveDefaultSql(ParamConfig cfg, SqlProviderKind provider = SqlProviderKind.SqlServer)
        {
            if (!string.IsNullOrWhiteSpace(cfg.DefaultSqlExpression))
                return cfg.DefaultSqlExpression;

            if (string.Equals(prop.Name, "Id", StringComparison.OrdinalIgnoreCase) && cfg.PropertyType.IsGuidType())
                return provider == SqlProviderKind.PostgreSql ? "DEFAULT gen_random_uuid()" : "DEFAULT NEWID()";

            return null;
        }
    }
}
