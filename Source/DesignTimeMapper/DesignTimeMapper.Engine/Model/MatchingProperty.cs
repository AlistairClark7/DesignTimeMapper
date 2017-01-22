using Microsoft.CodeAnalysis;

namespace DesignTimeMapper.Engine.Model
{
    public class MatchingProperty
    {
        public IPropertySymbol ClassToMapFromProperty { get; set; }
        public IPropertySymbol ClassToMapToProperty { get; set; }

        public bool DoNotMap { get; set; }
    }
}