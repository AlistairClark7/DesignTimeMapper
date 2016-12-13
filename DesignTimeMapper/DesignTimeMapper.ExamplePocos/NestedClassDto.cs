using DesignTimeMapper.Attributes;

namespace DesignTimeMapper.ExamplePocos
{
    [MapFrom(typeof(NestedClass))]
    public class NestedClassDto
    {
        public string Property1 { get; set; }
        public string NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }
}