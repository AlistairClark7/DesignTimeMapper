# DesignTimeMapper
Visual Studio extension for creating DTO/mapped objects.

## How to install ##

Install from nuget. Use the command `Install-Package DesignTimeMapper -Pre`

## How to use ##

Add the `MapFromAttribute` to a class with the type you want to map from.
E.g.


    [MapFrom(typeof(NestedClass))]
    public class NestedClassDto
    {
        public string Property1 { get; set; }
        public int NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }

If you want to exclude properties from being mapped then use the `DoNotMapAttribute`

E.g. 

    [MapFrom(typeof(NestedClass))]
    public class NestedClassDto
    {
        [DoNotMap]
        public string Property1 { get; set; }
        public int NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }

When the project is built a new class is added called `DesignTimeMapper.cs` which contains the mapper methods.

More details and features to come later!