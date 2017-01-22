using Microsoft.CodeAnalysis;

namespace DesignTimeMapper.Engine.Model
{
    public class MatchingProperty
    {
        public MatchingProperty(IPropertySymbol mapToPropertySymbol)
        {
            MapToProperty = mapToPropertySymbol;
        }

        public IPropertySymbol MapFromProperty { get; set; }
        public IPropertySymbol MapToProperty { get; set; }

        public bool DoNotMap { get; set; }
    }
}