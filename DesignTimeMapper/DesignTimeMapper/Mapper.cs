﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

namespace DesignTimeMapper
{
    public class Mapper
    {
        public async Task Map(string solutionPath, string projectName)
        {
            var msWorkspace = MSBuildWorkspace.Create();
            var solution = await msWorkspace.OpenSolutionAsync(solutionPath);
            var project = solution.Projects.First(p => p.Name == projectName);
            var compilation = await project.GetCompilationAsync();

            var mapperMethodGenerator = new MapperMethodGenerator();
            var mappedMethods = mapperMethodGenerator.CreateMapperMethods(compilation);

            var classMapper = new ClassMapper();
            var newClass = classMapper.CreateMapClass(msWorkspace, mappedMethods);

            var existing = project.Documents.FirstOrDefault(d => d.Name == "DesignTimeMapper.cs");

            var document = existing == null
                ? project.AddDocument("DesignTimeMapper.cs", newClass)
                : existing.WithText(newClass);

            msWorkspace.TryApplyChanges(document.Project.Solution);
        }
    }
}