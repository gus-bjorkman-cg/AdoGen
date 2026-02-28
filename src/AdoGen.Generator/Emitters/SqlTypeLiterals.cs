using System;
using System.Data;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

internal static class SqlTypeLiterals
{
    public static string ToSqlTypeLiteral(ParamConfig cfg)
    {
        if (cfg.DbType is null) throw new InvalidOperationException("Missing DbType");

        return cfg.DbType.Value.Provider == SqlProviderKind.PostgreSql
            ? ToPostgreTypeLiteral(cfg)
            : ToSqlServerTypeLiteral(cfg);
    }

    private static string ToSqlServerTypeLiteral(ParamConfig cfg)
    {
        var dbt = (SqlDbType)Enum.Parse(typeof(SqlDbType), cfg.DbType!.Value.EnumMember);
        return dbt switch
        {
            SqlDbType.NVarChar => cfg.Size is int s && s > 0 ? $"NVARCHAR({s})" : "NVARCHAR(MAX)",
            SqlDbType.VarChar => cfg.Size is int s2 && s2 > 0 ? $"VARCHAR({s2})" : "VARCHAR(MAX)",
            SqlDbType.NChar => cfg.Size is int s3 && s3 > 0 ? $"NCHAR({s3})" : "NCHAR(1)",
            SqlDbType.Char => cfg.Size is int s4 && s4 > 0 ? $"CHAR({s4})" : "CHAR(1)",
            SqlDbType.VarBinary => cfg.Size is int s5 && s5 > 0 ? $"VARBINARY({s5})" : "VARBINARY(MAX)",
            SqlDbType.Decimal => $"DECIMAL({(cfg.Precision ?? 18)},{(cfg.Scale ?? 2)})",
            _ => dbt.ToString().ToUpperInvariant()
        };
    }

    private static string ToPostgreTypeLiteral(ParamConfig cfg)
    {
        // Use NpgsqlDbType enum member names, but map to SQL type literals.
        return cfg.DbType!.Value.EnumMember switch
        {
            "Varchar" => cfg.Size is int s && s > 0 ? $"VARCHAR({s})" : "VARCHAR",
            "Text" => "TEXT",
            "Char" => cfg.Size is int s2 && s2 > 0 ? $"CHAR({s2})" : "CHAR(1)",
            "Bytea" => "BYTEA",
            "Numeric" => $"NUMERIC({(cfg.Precision ?? 18)},{(cfg.Scale ?? 2)})",
            "Boolean" => "BOOLEAN",
            "Smallint" => "SMALLINT",
            "Integer" => "INTEGER",
            "Bigint" => "BIGINT",
            "Real" => "REAL",
            "Double" => "DOUBLE PRECISION",
            "Uuid" => "UUID",
            "Timestamp" => "TIMESTAMP",
            "TimestampTz" => "TIMESTAMPTZ",
            "Date" => "DATE",
            "Time" => "TIME",
            "Varbit" => cfg.Size is int s3 && s3 > 0 ? $"VARBIT({s3})" : "VARBIT",
            // Fallback: use lower-case enum member as sql type name (best-effort).
            var x => x.ToLowerInvariant()
        };
    }
}