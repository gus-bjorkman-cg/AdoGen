using System;
using System.Data;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

internal interface ISqlTypeLiterals
{
    bool IsMatch(ParamConfig cfg);
    string Get(ParamConfig cfg);
}

internal sealed class SqlTypeLiteralsSqlServer : ISqlTypeLiterals
{
    private SqlTypeLiteralsSqlServer() {}
    public static SqlTypeLiteralsSqlServer Instance { get; } = new();
    
    public bool IsMatch(ParamConfig cfg) => cfg.DbType?.Provider is SqlProviderKind.SqlServer;

    public string Get(ParamConfig cfg) =>
        (SqlDbType)Enum.Parse(typeof(SqlDbType), cfg.DbType!.Value.EnumMember) switch
        {
            SqlDbType.NVarChar => cfg.Size is int s && s > 0 ? $"NVARCHAR({s})" : "NVARCHAR(MAX)",
            SqlDbType.VarChar => cfg.Size is int s2 && s2 > 0 ? $"VARCHAR({s2})" : "VARCHAR(MAX)",
            SqlDbType.NChar => cfg.Size is int s3 && s3 > 0 ? $"NCHAR({s3})" : "NCHAR(1)",
            SqlDbType.Char => cfg.Size is int s4 && s4 > 0 ? $"CHAR({s4})" : "CHAR(1)",
            SqlDbType.VarBinary => cfg.Size is int s5 && s5 > 0 ? $"VARBINARY({s5})" : "VARBINARY(MAX)",
            SqlDbType.Decimal => $"DECIMAL({(cfg.Precision ?? 18)},{(cfg.Scale ?? 2)})",
            _ => cfg.DbType!.Value.EnumMember.ToUpperInvariant()
        };
}

internal sealed class SqlTypeLiteralsPostgreSql : ISqlTypeLiterals
{
    private SqlTypeLiteralsPostgreSql() {}
    public static SqlTypeLiteralsPostgreSql Instance { get; } = new();
    
    public bool IsMatch(ParamConfig cfg) => cfg.DbType?.Provider is SqlProviderKind.PostgreSql;

    public string Get(ParamConfig cfg) =>
        cfg.DbType!.Value.EnumMember switch
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
            var x => x.ToLowerInvariant()
        };
}