using System.Collections.Generic;

namespace DesignTimeMapper.Settings
{
    public class DtmSettings
    {
        public List<string> SourceFiles { get; set; }
        public string DestinationProject { get; set; }
        public string MappedClassPrefix { get; set; }
        public string MappedClassSuffix { get; set; }
    }
}