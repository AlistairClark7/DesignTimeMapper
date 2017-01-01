namespace DesignTimeMapper.ExamplePocos
{
    public static class DtmExtensions
    {
        public static DesignTimeMapper.ExamplePocos.NestedClassDto MapToNestedClassDto(this NestedClass nestedclass)
        {
            return new DesignTimeMapper.ExamplePocos.NestedClassDto()
            {Property1 = nestedclass.Property1, NestedProperty1 = nestedclass.Nested.Property1, NestedProperty2 = nestedclass.Nested.Property2};
        }

        public static NestedClass MapToNestedClass(this DesignTimeMapper.ExamplePocos.NestedClassDto nestedclass)
        {
            return new NestedClass()
            {Property1 = nestedclass.Property1};
        }
    }
}