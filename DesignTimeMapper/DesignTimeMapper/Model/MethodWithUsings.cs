using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Model
{
    public class MethodWithUsings
    {
        public MemberDeclarationSyntax Method { get; set; }
        public List<string> Usings { get; set; }
    }
}