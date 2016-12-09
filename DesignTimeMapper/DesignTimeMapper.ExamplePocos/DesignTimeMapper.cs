using System.Device.Location;
using DesignTimeMapper.ExamplePocos;

namespace TestNameSpace
{
    public class DesignTimeMapper
    {
        public static AddressDto MapFrom(CivicAddress civicaddress)
        {
            return new AddressDto()
            {AddressLine1 = civicaddress.AddressLine1, AddressLine2 = civicaddress.AddressLine2, Building = civicaddress.Building, City = civicaddress.City, PostalCode = civicaddress.PostalCode, StateProvince = civicaddress.StateProvince};
        }
    }
}