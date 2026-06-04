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
    
    public static readonly DiagnosticDescriptor MissingProfile = new(
        id: "AG002",
        title: "Missing SqlProfile",
        messageFormat:
        "Type '{0}' has SqlProfile, create a class with SqlProfile<{0}>",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error, 
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MissingRequiredParameterConfig = new(
        id: "AG003",
        title: "Missing required parameter configuration",
        messageFormat:
        "Type '{0}' has property '{1}' of type '{2}' which requires additional configuration in its SqlProfile. Missing configuration: {3}.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error, 
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StringMissing = new(
        id: "AG004",
        title: "String property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has string property '{1}' without explicit SqlDbType and Size in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DecimalMissing = new(
        id: "AG005",
        title: "Decimal property requires Precision and Scale",
        messageFormat: "Type '{0}' has decimal property '{1}' without explicit Precision and Scale in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BinaryMissing = new(
        id: "AG006",
        title: "Binary property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has binary property '{1}' without explicit SqlDbType and Size in its SqlProfile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor NonConstantArg = new(
        id: "AG007",
        title: "Non-constant configuration argument",
        messageFormat: "Configuration for '{0}.{1}' uses a non-constant argument. Use a literal or const value.",
        category: "Usage",
        DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor MissingKey = new(
        id: "AG008",
        title: "Missing key configuration",
        messageFormat: "Type '{0}' has no key. Default key 'Id' not found. Update/Delete/Upsert cannot be generated.",
        category: "Reliability",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NoUpsertMatchKeys = new(
        id: "AG009",
        title: "Upsert cannot be generated",
        messageFormat:
        "Type '{0}' has no non-identity key to match on for MERGE. RuleFor Key(...) not also marked Identity(...).",
        category: "Reliability",
        DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor StaticNotAllowed = new(
        id: "AG010",
        title: "Static types not supported",
        messageFormat: "Type '{0}' is static. Static types are not supported for SQL source generation.",
        category: "Design",
        DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor InvalidAccessibility = new(
        id: "AG011",
        title: "Invalid type visibility",
        messageFormat: "Type '{0}' has invalid visibility. Only public and internal types are supported for SQL source generation.",
        category: "Design",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ConcurrencyTokenInvalidType = new(
        id: "AG012",
        title: "ConcurrencyToken unsupported type",
        messageFormat: "Type '{0}' has property '{1}' marked .ConcurrencyToken() but type '{2}' is not supported. Use int, long, or Guid.",
        category: "Design",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MultipleConcurrencyTokens = new(
        id: "AG013",
        title: "Multiple concurrency tokens",
        messageFormat: "Type '{0}' has more than one property marked .ConcurrencyToken(). Only one is allowed.",
        category: "Design",
        DiagnosticSeverity.Error, true);
}