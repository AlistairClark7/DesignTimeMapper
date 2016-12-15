using System.Collections.Generic;
using DesignTimeMapper.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Interface
{
    public interface IDtoMapperMethodGenerator
    {
        MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass,
            List<MemberDeclarationSyntax> properties, string newClassName);
    }
}