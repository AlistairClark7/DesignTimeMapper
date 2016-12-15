using System.Collections.Generic;
using DesignTimeMapper.Engine.Model;
using Microsoft.CodeAnalysis;

namespace DesignTimeMapper.Engine.Interface
{
    public interface IMapperMethodGenerator
    {
        IList<MethodWithUsings> CreateMapperMethods(Compilation compilation);
    }
}