using System.Device.Location;
using DesignTimeMapper.Attributes;

namespace DesignTimeMapper.ExamplePocos
{
    //[MapFrom(typeof(CivicAddress))]
    public class AddressDto
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Building { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string StateProvince { get; set; }
    }
}