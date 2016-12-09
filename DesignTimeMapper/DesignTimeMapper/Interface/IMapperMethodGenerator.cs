using System.Collections.Generic;
using DesignTimeMapper.Engine.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.Interface
{
    public interface IMapperMethodGenerator
    {
        IList<MethodWithUsings> CreateMapperMethods(Compilation compilation);

        MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass,
            List<MemberDeclarationSyntax> properties, string newClassName);
    }
}