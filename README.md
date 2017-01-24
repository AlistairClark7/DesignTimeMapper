# DesignTimeMapper
NuGet package for creating object-object mapping methods.

## How to install ##

Install from NuGet. Use the command `Install-Package DesignTimeMapper`

## What is it? ##

It's like AutoMapper. But instead of the mappings happening at run time, they are created at compile time using Roslyn to generate extension methods.

## How to use ##

Add the `MapFromAttribute` to a class with the type you want to map from.
E.g. given these classes
    
    public class ClassWithNesting
    {
        public string Property1 { get; set; }
        public SimpleClass Nested { get; set; }
    }
    
    public class SimpleClass
    {
        public int Property1 { get; set; }
        public string Property2 { get; set; }
    }

    [MapFrom(typeof(ClassWithNesting))]
    public class ClassWithNestingDto
    {
        public string Property1 { get; set; }
        public int NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }

The result of the mapping will be 
    
    public static DesignTimeMapper.ExamplePocos.ClassWithNestingDto MapToNestedClassDto(this ClassWithNesting nestedclass)
    {
        return new DesignTimeMapper.ExamplePocos.ClassWithNestingDto()
        {Property1 = nestedclass.Property1, NestedProperty1 = nestedclass.Nested.Property1, NestedProperty2 = nestedclass.Nested.Property2};
    }

    public static ClassWithNesting MapToNestedClass(this DesignTimeMapper.ExamplePocos.ClassWithNestingDto nestedclass)
    {
        return new ClassWithNesting()
        {Property1 = nestedclass.Property1, Nested = new DesignTimeMapper.ExamplePocos.SimpleClass()
        {Property1 = nestedclass.NestedProperty1, Property2 = nestedclass.NestedProperty2}};
    }

If you want to exclude properties from being mapped then use the `DoNotMapAttribute`

E.g. 
    
    [MapFrom(typeof(ClassWithNesting))]
    public class ClassWithNestingDto
    {
        [DoNotMap]
        public string Property1 { get; set; }
        public int NestedProperty1 { get; set; }
        public string NestedProperty2 { get; set; }
    }

Will result in a mapping of 

    public static DesignTimeMapper.ExamplePocos.ClassWithNestingDto MapToClassWithNestingDto(this ClassWithNesting classwithnesting)
    {
        return new DesignTimeMapper.ExamplePocos.ClassWithNestingDto()
        {NestedProperty1 = classwithnesting.Nested.Property1, NestedProperty2 = classwithnesting.Nested.Property2};
    }

    public static ClassWithNesting MapToClassWithNesting(this DesignTimeMapper.ExamplePocos.ClassWithNestingDto classwithnesting)
    {
        return new ClassWithNesting()
        {Nested = new DesignTimeMapper.ExamplePocos.SimpleClass()
        {Property1 = classwithnesting.NestedProperty1, Property2 = classwithnesting.NestedProperty2}};
    }

When the project is built a new class is added called `DtmExtenstions.cs` which contains the mapper methods.

More details and features to come later!

## How does it work? ##

When you install the nuget package, it will add a `PreBuildEvent` in a `.targets` file which looks like:

`<PreBuildEvent>"$(SolutionDir)packages\DesignTimeMapper.0.4.1\build\DesignTimeMapper.CommandLine.exe" "$(SolutionPath)" "$(MSBuildProjectName)"</PreBuildEvent>`

Each time the project is built, the command line tool uses Roslyn to look for any classes with the `[MapFrom]` attribute. For each of these classes, it will create two extension methods to map from and to each class. That's about it really.

## The future ##

At the moment it's quite basic. In the future it will do some more things like:
- Checking that the mapped values are both readable/writable
- Null checking (with nice exception throwing)
- Generation of `Expression<Func<...>>`s for things like Entity Framework queries 
- Optional type coersion (e.g. automatic generation of TryParse to go from a string to an int)
- Custom pattern matching
- Configurable extension file/class/method names
- Performance improvements - it's probably not as effecient as it could be. Build time on projects with many mappings may currently be slow
- Any other features that people request or are willing to contribute

## Known issues ##

There are no checks for types during the mapping - so it may generate code that does not compile.
Does not currently check that each property is writable - so again you may get generated code that does not compile.
This is an early version so there are likely to be problems. Please open an issue with any details of the problem.