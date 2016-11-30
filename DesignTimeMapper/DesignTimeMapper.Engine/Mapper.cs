using System.Threading.Tasks;
using DesignTimeMapper.Engine.Settings;
using Microsoft.CodeAnalysis.MSBuild;

namespace DesignTimeMapper.Engine
{
    public class Mapper
    {
        public async Task Map(DtmSettings settings)
        {
            var workSpace = MSBuildWorkspace.Create();
            var project = await workSpace.OpenProjectAsync(settings.DestinationProject).ConfigureAwait(false);
            

        }
    }
}