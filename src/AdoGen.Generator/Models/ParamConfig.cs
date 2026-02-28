using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Models;

internal sealed class ParamConfig
{
    public required string PropertyName { get; set; }
    public required ITypeSymbol PropertyType { get; set; }
    public required string ParameterName { get; set; }
    public DbTypeRef? DbType { get; set; }
    public int? Size { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool? IsNullable { get; set; }
    public string? DefaultSqlExpression { get; set; }
    public string SqlTypeLiteral { get; set; } = "";
}