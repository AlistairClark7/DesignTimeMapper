using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.Model
{
    public class MethodWithUsings
    {
        public MemberDeclarationSyntax Method { get; set; }
        public List<INamespaceSymbol> Usings { get; set; }
    }
}