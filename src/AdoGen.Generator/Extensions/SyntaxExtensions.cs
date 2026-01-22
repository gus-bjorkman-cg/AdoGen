using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Extensions;

internal static class SyntaxExtensions
{
    public static string? TryGetPropertyNameFromLambdaStrict(this LambdaExpressionSyntax lambda, SemanticModel model)
    {
        var body = lambda.Body;
        if (body is MemberAccessExpressionSyntax mae)
        {
            var symbol = model.GetSymbolInfo(mae).Symbol;
            if (symbol is IPropertySymbol ps) return ps.Name;
        }
        else if (body is IdentifierNameSyntax id)
        {
            var symbol = model.GetSymbolInfo(id).Symbol;
            if (symbol is IPropertySymbol ps) return ps.Name;
        }
        return null;
    }
}
