using System.Data;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

internal static class SqlTypeLiterals
{
    public static string ToSqlTypeLiteral(ParamConfig cfg)
    {
        var dbt = cfg.DbType!.Value;
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
}