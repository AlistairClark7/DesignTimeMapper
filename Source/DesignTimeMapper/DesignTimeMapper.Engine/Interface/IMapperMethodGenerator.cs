using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.Interface
{
    public interface IMapperMethodGenerator
    {
        IList<MemberDeclarationSyntax> CreateMapperMethods(Compilation compilation);
    }
}