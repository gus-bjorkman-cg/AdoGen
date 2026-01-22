using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Models;

public sealed record ChainMethod(string Name, SeparatedSyntaxList<ArgumentSyntax> Args, SyntaxNode Node);