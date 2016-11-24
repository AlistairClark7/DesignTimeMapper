﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesignTimeMapper.Engine.Tests.Unit
{
    [TestClass]
    public class SimpleClassTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var template =
                @"namespace DesignTimeMapper.ExamplePocos
                {
                    public class SimpleClass
                    {
                        public int Property1 { get; set; }
                        public string Property2 { get; set; }
                    }
                }";

            var newNamespace = "Mapped";
            var newClassName = "SimpleClassDto";

            var mappedClass = new Mapper().CreateMapClass(template, newNamespace, newClassName);

            var expected = $@"namespace {newNamespace}
                {{
                    public class {newClassName}
                    {{
                        public int Property1 {{ get; set; }}
                        public string Property2 {{ get; set; }}
                        public static SimpleClassDto Create(SimpleClass simpleclass)
                        {{
                            return new SimpleClassDto()
                            {{
                                Property1 = simpleclass.Property1, 
                                Property2 = simpleclass.Property2
                            }};
                        }}
                    }}
                }}";
            
            Debug.WriteLine(mappedClass);

            Assert.AreEqual(Regex.Replace(expected, @"\s+", ""), Regex.Replace(mappedClass, @"\s+", ""));
        }

        [TestMethod]
        public void Notepad()
        {
        }
    }
}