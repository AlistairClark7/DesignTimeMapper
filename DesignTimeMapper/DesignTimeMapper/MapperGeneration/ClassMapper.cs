using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Extensions;
using DesignTimeMapper.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DesignTimeMapper.MapperGeneration
{
    public class ClassMapper
    {
        public SourceText CreateMapClass(IEnumerable<MethodWithUsings> methods, string namespaceName)
        {
            var newClassName = "DesignTimeMapper";
            
            var newClass = SyntaxFactory.CompilationUnit()
                .WithMembers
                (
                    SyntaxFactory.List
                    (
                        new MemberDeclarationSyntax[]
                        {
                            SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                    SyntaxFactory.ClassDeclaration(newClassName)
                                        .WithMembers
                                        (
                                            SyntaxFactory.List
                                            (
                                                methods.Select(m => m.Method)
                                            )
                                        ).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                ))
                        }
                    )
                );
            
            foreach (var methodWithUsingse in methods)
            {
                foreach (var u in methodWithUsingse.Usings)
                {
                    newClass = newClass.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u.GetFullMetadataName())));
                }
            }

            return newClass.NormalizeWhitespace().GetText();
        }
    }
}