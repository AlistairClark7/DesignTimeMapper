using System.Collections.Generic;
using DesignTimeMapper.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Interface
{
    public interface IMapperMethodGenerator
    {
        IList<MethodWithUsings> CreateMapperMethods(Compilation compilation);

        MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass,
            List<MemberDeclarationSyntax> properties, string newClassName);
    }
}