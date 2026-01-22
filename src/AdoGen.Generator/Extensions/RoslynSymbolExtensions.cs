using System;
using System.Data;
using Microsoft.CodeAnalysis;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Extensions;

internal static class RoslynSymbolExtensions
{
    extension(ITypeSymbol t)
    {
        public bool IsString() => t.SpecialType == SpecialType.System_String;
        public bool IsDecimal() => t.SpecialType == SpecialType.System_Decimal;
        public bool IsByteArray() => t is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };
        
        public bool IsGuidType()
        {
            // unwrap Nullable<T>
            if (t is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nt)
                t = nt.TypeArguments[0];

            return t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Guid";
        }


        public SqlDbType MapDefaultSqlDbType()
        {
            // unwrap Nullable<T>
            if (t is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nt) t = nt.TypeArguments[0];

            return t.SpecialType switch
            {
                SpecialType.System_Boolean => SqlDbType.Bit,
                SpecialType.System_Byte    => SqlDbType.TinyInt,
                SpecialType.System_Int16   => SqlDbType.SmallInt,
                SpecialType.System_Int32   => SqlDbType.Int,
                SpecialType.System_Int64   => SqlDbType.BigInt,
                SpecialType.System_Single  => SqlDbType.Real,
                SpecialType.System_Double  => SqlDbType.Float,
                SpecialType.System_Decimal => SqlDbType.Decimal,      // requires precision/scale
                SpecialType.System_String  => SqlDbType.NVarChar,     // requires size
                _ => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) switch
                {
                    "global::System.Guid"     => SqlDbType.UniqueIdentifier,
                    "global::System.DateTime" => SqlDbType.DateTime2, // your default
                    "global::System.DateOnly" => SqlDbType.Date,
                    "global::System.TimeOnly" => SqlDbType.Time,
                    "global::System.Byte[]"   => SqlDbType.VarBinary, // requires size
                    _ => SqlDbType.Variant
                }
            };
        }
    }

    extension(IPropertySymbol prop)
    {
        public bool IsNullableProperty(ParamConfig cfg)
        {
            if (cfg.IsNullable is bool forced) return forced;

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

        
        public string? ResolveDefaultSql(ParamConfig cfg)
        {
            if (!string.IsNullOrWhiteSpace(cfg.DefaultSqlExpression))
                return cfg.DefaultSqlExpression;

            if (string.Equals(prop.Name, "Id", StringComparison.OrdinalIgnoreCase) && cfg.PropertyType.IsGuidType())
                return "DEFAULT NEWID()";

            return null;
        }
    }
}
