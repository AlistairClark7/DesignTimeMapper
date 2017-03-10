namespace DesignTimeMapper.ExamplePocos
{
    public static class DtmExtensions
    {
        public static DesignTimeMapper.ExamplePocos.AddressDto MapToAddressDto(this System.Device.Location.CivicAddress civicaddress)
        {
            return new DesignTimeMapper.ExamplePocos.AddressDto()
            {AddressLine1 = civicaddress.AddressLine1, AddressLine2 = civicaddress.AddressLine2, Building = civicaddress.Building, City = civicaddress.City, PostalCode = civicaddress.PostalCode, StateProvince = civicaddress.StateProvince};
        }

        public static System.Device.Location.CivicAddress MapToCivicAddress(this DesignTimeMapper.ExamplePocos.AddressDto civicaddress)
        {
            return new System.Device.Location.CivicAddress()
            {AddressLine1 = civicaddress.AddressLine1, AddressLine2 = civicaddress.AddressLine2, Building = civicaddress.Building, City = civicaddress.City, PostalCode = civicaddress.PostalCode, StateProvince = civicaddress.StateProvince};
        }

        public static DesignTimeMapper.ExamplePocos.NestedClassDto MapToNestedClassDto(this NestedClass nestedclass)
        {
            return new DesignTimeMapper.ExamplePocos.NestedClassDto()
            {NestedProperty1 = nestedclass.Nested.Property1, NestedProperty2 = nestedclass.Nested.Property2};
        }

        public static NestedClass MapToNestedClass(this DesignTimeMapper.ExamplePocos.NestedClassDto nestedclass)
        {
            return new NestedClass()
            {Nested = new DesignTimeMapper.ExamplePocos.SimpleClass()
            {Property1 = nestedclass.NestedProperty1, Property2 = nestedclass.NestedProperty2}};
        }
    }
}