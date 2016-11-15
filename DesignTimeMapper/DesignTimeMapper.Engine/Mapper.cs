using System.Collections.Generic;
using System.Management.Instrumentation;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace DesignTimeMapper.Engine
{
    public class Mapper
    {
        public string CreateMapClass(string classText, string newNamespaceName, string newClassName)
        {
            var workspace = new AdhocWorkspace();
            var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

            var oldTree = CSharpSyntaxTree.ParseText(classText);
            var originalClass = (CompilationUnitSyntax)oldTree.GetRoot();

            var properties = new List<PropertyDeclarationSyntax>();
            foreach (var member in originalClass.Members)
            {
                var ns = member as NamespaceDeclarationSyntax;
                if (ns != null)
                {
                    foreach (var nsMember in ns.Members)
                    {
                        properties.AddRange(TryProcessClass(nsMember));
                    }
                }

                properties.AddRange(TryProcessClass(member));
            }

            var newClass = generator.ClassDeclaration(newClassName, null, Accessibility.Public, DeclarationModifiers.None, null, null, properties);
            var newNamespace = generator.NamespaceDeclaration(newNamespaceName, newClass);

            return generator.CompilationUnit(newNamespace).NormalizeWhitespace().ToString();
        }

        private static IEnumerable<PropertyDeclarationSyntax> TryProcessClass(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            var c = memberDeclarationSyntax as ClassDeclarationSyntax;
            if (c != null)
            {
                foreach (var cMember in c.Members)
                {
                    var property = cMember as PropertyDeclarationSyntax;
                    if (property != null)
                    {
                        yield return property;
                    }
                }
            }
        }
    }
}