using System;
using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Engine.Interface;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DesignTimeMapper.Engine
{
    public class ClassMapper
    {
        private IMapperMethodGenerator _mapperMethodGenerator = new MapperMethodGenerator();


        public string CreateMapClass(string classText, string newNamespaceName, string newClassPrefix, string newClassSuffix)
        {
            var workspace = new AdhocWorkspace();
            var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

            var oldTree = CSharpSyntaxTree.ParseText(classText);
            var originalClass = (CompilationUnitSyntax) oldTree.GetRoot();


            var compilationUnitSyntaxs = new List<CompilationUnitSyntax>();
            foreach (var member in originalClass.Members)
            {
                var ns = member as NamespaceDeclarationSyntax;
                if (ns != null)
                {
                    foreach (var nsMember in ns.Members)
                    {
                        var compilationUnitSyntax = CreateCompilationUnitSyntax(nsMember, newNamespaceName, newClassSuffix, newClassPrefix);
                        compilationUnitSyntaxs.Add(compilationUnitSyntax);
                        return compilationUnitSyntax.ToFullString();
                    }
                }
                else
                {
                    var compilationUnitSyntax = CreateCompilationUnitSyntax(member, newNamespaceName, newClassSuffix, newClassPrefix);
                    compilationUnitSyntaxs.Add(compilationUnitSyntax);
                    return compilationUnitSyntax.ToFullString();
                }
            }
            
            var newNamespace = generator.NamespaceDeclaration(newNamespaceName, compilationUnitSyntaxs);
            return generator.CompilationUnit(newNamespace).NormalizeWhitespace().ToString();
        }

        private CompilationUnitSyntax CreateCompilationUnitSyntax(MemberDeclarationSyntax nsMember, string newNamespaceName, string newClassSuffix, string newClassPrefix)
        {
            var properties = new List<MemberDeclarationSyntax>();
            var c = nsMember as ClassDeclarationSyntax;

            if(c == null)
                throw new NotImplementedException("Handle case when this isn't a class");

            var newClassName = $"{newClassPrefix}{c.Identifier}{newClassSuffix}";

            properties.AddRange(TryGetClassProperties(nsMember));
            var mapperMethod = _mapperMethodGenerator.CreateMapperMethod(nsMember, properties, newClassName);
            var compilationUnitSyntax = CompilationUnit()
                .WithMembers
                (
                    List
                    (
                        new MemberDeclarationSyntax[]
                        {
                            NamespaceDeclaration(IdentifierName(newNamespaceName)).WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ClassDeclaration(newClassName)
                                        .WithMembers
                                        (
                                            List
                                            (
                                                properties.Concat(new[] {mapperMethod})
                                            )
                                        ).WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                ))
                        }
                    )
                )
                .NormalizeWhitespace();
            return compilationUnitSyntax;
        }

        private static IEnumerable<PropertyDeclarationSyntax> TryGetClassProperties(
            MemberDeclarationSyntax memberDeclarationSyntax)
        {
            return TryGetClassProperties(memberDeclarationSyntax as ClassDeclarationSyntax);
        }

        private static IEnumerable<PropertyDeclarationSyntax> TryGetClassProperties(
            ClassDeclarationSyntax memberDeclarationSyntax)
        {
            if (memberDeclarationSyntax != null)
                foreach (var cMember in memberDeclarationSyntax.Members)
                {
                    var property = cMember as PropertyDeclarationSyntax;
                    if (property != null)
                        yield return property;
                }
        }
    }
}