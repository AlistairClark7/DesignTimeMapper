﻿using System;

namespace DesignTimeMapper.Engine.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MapFromAttribute : Attribute
    {
        public Type Type { get; set; }

        public MapFromAttribute(Type type)
        {
            Type = type;
        }
    }
}