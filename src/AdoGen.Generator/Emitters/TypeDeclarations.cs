using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// Shapes the partial declarations emitted for a DTO: its keyword (class / record / struct / record struct)
/// and the partial containing types wrapped around nested DTOs.
/// </summary>
internal static class TypeDeclarations
{
    public static string Keyword(INamedTypeSymbol type) => type switch
    {
        { IsRecord: true, IsValueType: true } => "record struct",
        { IsRecord: true } => "record",
        { TypeKind: TypeKind.Struct } => "struct",
        { TypeKind: TypeKind.Interface } => "interface",
        _ => "class"
    };

    /// <summary>
    /// Lowest accessibility along the containing-type chain, for top-level types that expose the DTO.
    /// </summary>
    public static string EffectiveAccessibility(INamedTypeSymbol type)
    {
        for (var t = type; t is not null; t = t.ContainingType)
        {
            if (t.DeclaredAccessibility is not Accessibility.Public) return "internal";
        }

        return "public";
    }

    /// <summary>Containing type names joined by <paramref name="separator"/> and terminated by it; empty for top-level types.</summary>
    public static string ContainerPrefix(INamedTypeSymbol type, string separator = ".")
    {
        var containers = Containers(type);
        if (containers.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var container in containers) sb.Append(container.Name).Append(separator);
        return sb.ToString();
    }

    /// <summary>Hint name for a generated file, prefixed with the containing types of nested DTOs.</summary>
    public static string HintName(INamedTypeSymbol type, string suffix) =>
        $"{ContainerPrefix(type)}{type.Name}.{suffix}";

    /// <summary>
    /// Wraps everything after the <c>namespace X;</c> line of <paramref name="source"/> in partial declarations
    /// of the DTO's containing types. Content is not re-indented so raw SQL string literals stay unchanged.
    /// </summary>
    public static string WrapInContainers(INamedTypeSymbol type, string source)
    {
        var containers = Containers(type);
        if (containers.Count == 0) return source;

        var namespaceLine = source.IndexOf("\nnamespace ", System.StringComparison.Ordinal);
        var bodyStart = namespaceLine < 0 ? 0 : source.IndexOf('\n', namespaceLine + 1) + 1;

        var sb = new StringBuilder(source.Length + containers.Count * 48);
        sb.Append(source, 0, bodyStart);
        foreach (var container in containers)
            sb.Append("partial ").Append(Keyword(container)).Append(' ').Append(container.Name).Append("\n{\n");
        sb.Append(source, bodyStart, source.Length - bodyStart);
        if (source.Length > 0 && source[source.Length - 1] != '\n') sb.Append('\n');
        for (var i = 0; i < containers.Count; i++) sb.Append("}\n");

        return sb.ToString();
    }

    /// <summary>
    /// Appends <c>Name: default!</c> for ignored properties bound through a constructor parameter, so the
    /// generated <c>new T(...)</c> call still resolves. Each argument line ends with a comma.
    /// </summary>
    public static void AppendIgnoredConstructorArguments(INamedTypeSymbol dto, AdoGen.Generator.Models.ProfileInfo profileInfo, StringBuilder read)
    {
        if (profileInfo.IgnoredProperties.IsDefaultOrEmpty) return;

        foreach (var ignored in profileInfo.IgnoredProperties)
        {
            var isCtorParameter = HasConstructorParameter(dto, ignored);
            if (isCtorParameter) read.AppendLine($"            {ignored}: default!,");
        }
    }
    private static bool HasConstructorParameter(INamedTypeSymbol type, string name)
    {
        foreach (var ctor in type.InstanceConstructors)
        foreach (var parameter in ctor.Parameters)
            if (parameter.Name == name) return true;

        return false;
    }

    private static List<INamedTypeSymbol> Containers(INamedTypeSymbol type)
    {
        var containers = new List<INamedTypeSymbol>();
        for (var t = type.ContainingType; t is not null; t = t.ContainingType) containers.Insert(0, t);
        return containers;
    }
}
