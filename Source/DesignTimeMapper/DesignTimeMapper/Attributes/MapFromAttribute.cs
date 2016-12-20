using System;

namespace DesignTimeMapper.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MapFromAttribute : Attribute
    {
        public Type[] Types { get; set; }

        public MapFromAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}