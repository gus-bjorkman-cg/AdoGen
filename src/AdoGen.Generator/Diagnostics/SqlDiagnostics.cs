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
        title: "Missing profile",
        messageFormat: "Type '{0}' has no {1}. Create a class deriving from {1}<{0}>.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error, 
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StringMissing = new(
        id: "AG004",
        title: "String property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has string property '{1}' without an explicit type and size in its profile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DecimalMissing = new(
        id: "AG005",
        title: "Decimal property requires Precision and Scale",
        messageFormat: "Type '{0}' has decimal property '{1}' without explicit Precision and Scale in its profile",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BinaryMissing = new(
        id: "AG006",
        title: "Binary property requires explicit SqlDbType and Size",
        messageFormat: "Type '{0}' has binary property '{1}' without an explicit type and size in its profile",
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

    public static readonly DiagnosticDescriptor AbstractNotAllowed = new(
        id: "AG014",
        title: "Abstract types not supported",
        messageFormat: "Type '{0}' is abstract. The generated mapper must construct instances; implement the AdoGen interfaces on a concrete type instead.",
        category: "Design",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NestedTypeNotSupported = new(
        id: "AG015",
        title: "Nested types not supported",
        messageFormat: "Type '{0}' is nested inside '{1}'. Declare AdoGen DTOs as top-level types.",
        category: "Design",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidColumnName = new(
        id: "AG016",
        title: "Invalid column name",
        messageFormat: "Column name '{2}' configured for '{0}.{1}' is not a valid identifier. Use letters, digits and underscores only, not starting with a digit.",
        category: "Usage",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        id: "AG017",
        title: "Unsupported property type",
        messageFormat: "Type '{0}' has property '{1}' of type '{2}' which has no default database type. Configure it with .Type(...) or use a supported type.",
        category: "Usage",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MultipleProfiles = new(
        id: "AG018",
        title: "Multiple profiles for the same type",
        messageFormat: "Type '{0}' has {1} {2}<{0}> classes. Exactly one profile per type and provider is allowed.",
        category: "Design",
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedProfileExpression = new(
        id: "AG019",
        title: "Unsupported profile expression",
        messageFormat: "Profile call '{1}' for '{0}' must use an inline lambda that selects a mapped property of '{0}'",
        category: "Usage",
        DiagnosticSeverity.Error, true);
}
