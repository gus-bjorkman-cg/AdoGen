using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Models;

internal sealed record ChainMethod(string Name, SeparatedSyntaxList<ArgumentSyntax> Args, SyntaxNode Node);