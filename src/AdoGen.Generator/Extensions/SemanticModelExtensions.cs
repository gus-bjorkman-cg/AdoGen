using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Extensions;

internal static class SemanticModelExtensions
{
    extension(SemanticModel model)
    {
        public bool TryGetConstEnumArg<TEnum>(ExpressionSyntax arg, CancellationToken ct, out TEnum value)
            where TEnum : struct, Enum
        {
            var constant = model.GetConstantValue(arg, ct);
            if (constant is { HasValue: true, Value: int i } && Enum.IsDefined(typeof(TEnum), i))
            {
                value = (TEnum)Enum.ToObject(typeof(TEnum), i);
                return true;
            }

            var sym = model.GetSymbolInfo(arg, ct).Symbol;
            if (sym is IFieldSymbol { HasConstantValue: true, ConstantValue: int j } field && field.ContainingType?.Name == typeof(TEnum).Name)
            {
                value = (TEnum)Enum.ToObject(typeof(TEnum), j);
                return true;
            }

            value = default!;
            return false;
        }

        public bool TryGetConstInt(ExpressionSyntax arg, CancellationToken ct, out int value)
        {
            var cv = model.GetConstantValue(arg, ct);
            if (cv is { HasValue: true, Value: int i }) { value = i; return true; }

            var sym = model.GetSymbolInfo(arg, ct).Symbol;
            if (sym is IFieldSymbol { HasConstantValue: true } field && field.ConstantValue is int j) { value = j; return true; }

            value = default;
            return false;
        }

        public bool TryGetConstByte(ExpressionSyntax arg, CancellationToken ct, out byte value)
        {
            var cv = model.GetConstantValue(arg, ct);
            if (cv.HasValue && cv.Value is byte b) { value = b; return true; }

            var sym = model.GetSymbolInfo(arg, ct).Symbol;
            if (sym is IFieldSymbol { HasConstantValue: true } field && field.ConstantValue is byte bb) { value = bb; return true; }

            value = default;
            return false;
        }

        public bool TryGetConstString(ExpressionSyntax arg, CancellationToken ct, out string? value)
        {
            var cv = model.GetConstantValue(arg, ct);
            if (cv is { HasValue: true, Value: string s })
            {
                value = s; 
                return true;
            }

            var sym = model.GetSymbolInfo(arg, ct).Symbol;
            if (sym is IFieldSymbol { HasConstantValue: true, ConstantValue: string ss })
            {
                value = ss; 
                return true;
            }
            
            if (arg is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" }, ArgumentList.Arguments.Count: 1 } inv)
            {
                value = inv.ArgumentList.Arguments[0].Expression.ToString();
                return true;
            }

            value = null;
            return false;
        }
    }
}