using System.Threading.Tasks;
using DesignTimeMapper.MapperGeneration;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesignTimeMapper.Tests.Integration
{
    [TestClass]
    public class MapperMethodGeneratorIntegrationTests
    {
        private const string ProjectFilePath = @"C:\Development\DesignTimeMapper\DesignTimeMapper\DesignTimeMapper.ExamplePocos\DesignTimeMapper.ExamplePocos.csproj";

        [TestMethod]
        public async Task TestMethod1()
        {
            var msWorkspace = MSBuildWorkspace.Create();

            //TODO - not hardcoded
            var project = await msWorkspace.OpenProjectAsync(ProjectFilePath);
            var compilation = await project.GetCompilationAsync();

            var mapperMethodGenerator = new MapperMethodGenerator();
            var mappedMethods = mapperMethodGenerator.CreateMapperMethods(compilation);
        }
    }
}