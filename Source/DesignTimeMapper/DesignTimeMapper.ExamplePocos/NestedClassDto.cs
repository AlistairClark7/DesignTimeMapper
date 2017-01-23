using DesignTimeMapper.Attributes;

namespace DesignTimeMapper.ExamplePocos
{
    [MapFrom(typeof(NestedClass))]
    public class NestedClassDto
    {
        [DoNotMap]
        public string Property1 { get; set; }
        public int NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }
}