using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Parsing.PostgreSql;
using AdoGen.Generator.Parsing.SqlServer;

namespace AdoGen.Generator.Parsing;

internal static class ConfigureChainParser
{
    private static readonly List<IChainMethodHandler> Handlers =
    [
        // Shared (provider-agnostic)
        SizeChainHandler.Instance,
        PrecisionChainHandler.Instance,
        ScaleChainHandler.Instance,
        NameChainHandler.Instance,
        NullableChainHandler.Instance,
        NotNullChainHandler.Instance,
        DefaultValueChainHandler.Instance,
        // SQL Server
        TypeChainHandlerSqlServer.Instance,
        NVarCharChainHandlerSqlServer.Instance,
        VarCharChainHandlerSqlServer.Instance,
        NCharChainHandlerSqlServer.Instance,
        CharChainHandlerSqlServer.Instance,
        VarBinaryChainHandlerSqlServer.Instance,
        DecimalChainHandlerSqlServer.Instance,
        // PostgreSQL
        TypeChainHandlerNpgsql.Instance,
        VarcharChainHandlerNpgsql.Instance,
        TextChainHandlerNpgsql.Instance,
        ByteaChainHandlerNpgsql.Instance,
        DecimalChainHandlerNpgsql.Instance
    ];

    public static void ParseConfigureRootAndForwardChain(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        IReadOnlyDictionary<string, IPropertySymbol> dtoProps,
        InvocationExpressionSyntax configureInvocation,
        Dictionary<string, ParamConfig> configs,
        SqlProviderKind provider,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct)
    {
        var lambda = (LambdaExpressionSyntax)configureInvocation.ArgumentList.Arguments[0].Expression;
        var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);

        if (propName is null || !dtoProps.TryGetValue(propName, out var propSymbol)) return;

        var cfg = new ParamConfig
        {
            PropertyName = propName,
            PropertyType = propSymbol.Type,
            ParameterName = propName
        };

        var chainMethods = new List<ChainMethod>();
        var current = configureInvocation.Parent;

        while (current is MemberAccessExpressionSyntax nextMae)
        {
            if (nextMae.Parent is InvocationExpressionSyntax nextCall)
            {
                chainMethods.Add(new ChainMethod(nextMae.Name.Identifier.Text, nextCall.ArgumentList.Arguments, nextCall));
                current = nextCall.Parent;
            }
            else break;
        }

        foreach (var chain in chainMethods)
        {
            for (var i = 0; i < Handlers.Count; i++)
            {
                if (Handlers[i].IsMatch(provider, chain.Name))
                {
                    Handlers[i].Handle(model, dtoType, propName, chain, cfg, diagnosticsBuilder, ct);
                    break;
                }
            }
        }

        configs[cfg.PropertyName] = cfg;
    }
}
