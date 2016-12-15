using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.Interface
{
    public interface IDtoMapperMethodGenerator
    {
        MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass,
            List<MemberDeclarationSyntax> properties, string newClassName);
    }
}