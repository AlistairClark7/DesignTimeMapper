# DesignTimeMapper
NuGet package for creating object-object mapping methods.

## How to install ##

Install from NuGet. Use the command `Install-Package DesignTimeMapper`

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

When the project is built a new class is added called `DtmExtenstions.cs` which contains the mapper methods.

More details and features to come later!

## Known issues ##

There are no checks for types during the mapping - so it may generate code that does not compile.
Does not currently check that each property is writable - so again you may get generated code that does not compile.
This is an early version so there are likely to be problems. Please open an issue with any details of the problem.