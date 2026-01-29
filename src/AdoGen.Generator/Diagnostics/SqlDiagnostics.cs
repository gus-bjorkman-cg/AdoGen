using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Diagnostics;

internal static class SqlDiagnostics
{
    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "AG001",
        title: "Type must be partial",
        messageFormat: "Type '{0}' must be declared partial to enable SQL source generation",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StringMissing = new(
        id: "AG002",
        title: "String property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has string property '{1}' without explicit SqlDbType and Size in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DecimalMissing = new(
        id: "AG003",
        title: "Decimal property requires Precision and Scale",
        messageFormat: "Type '{0}' has decimal property '{1}' without explicit Precision and Scale in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BinaryMissing = new(
        id: "AG004",
        title: "Binary property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has binary property '{1}' without explicit SqlDbType and Size in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UsesFallbackGetFieldValue = new(
        id: "AG005",
        title: "Mapper uses GetFieldValue<T>",
        messageFormat: "Mapper for '{0}.{1}' uses GetFieldValue<T>; prefer a typed getter for primitives for better performance",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MissingSqlResultInterface = new(
        id: "AG006",
        title: "Cannot find ISqlResult interface",
        messageFormat: "Could not resolve '{0}'. Ensure AdoGen.Abstractions is referenced.",
        category: "Usage",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NonConstantArg = new(
        id: "AG007",
        title: "Non-constant configuration argument",
        messageFormat: "Configuration for '{0}.{1}' uses a non-constant argument. Use a literal or const value.",
        category: "Usage",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingDomainInterface = new(
        id: "AG008",
        title: "Cannot find ISqlDomainModel<T>",
        messageFormat: "Could not resolve ISqlDomainModel<T>. Ensure AdoGen.Abstractions is referenced.",
        category: "Usage",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingKey = new(
        id: "AG009",
        title: "Missing key configuration",
        messageFormat: "Type '{0}' has no key. Default key 'Id' not found. Update/Delete/Upsert cannot be generated.",
        category: "Reliability",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NoUpsertMatchKeys = new(
        id: "AG010",
        title: "Upsert cannot be generated",
        messageFormat:
        "Type '{0}' has no non-identity key to match on for MERGE. RuleFor Key(...) not also marked Identity(...).",
        category: "Reliability",
        DiagnosticSeverity.Warning, true);
}