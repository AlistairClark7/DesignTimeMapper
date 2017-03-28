using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Engine.Extensions;
using DesignTimeMapper.Engine.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DesignTimeMapper.Engine.MapperGeneration
{
    public class ClassMapper
    {
        public const string MapperClassName = "DtmExtensions";

        public SourceText CreateMapClass(IEnumerable<MemberDeclarationSyntax> methods, string namespaceName)
        {
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
                                        SyntaxFactory.ClassDeclaration(MapperClassName)
                                            .WithMembers
                                            (
                                                SyntaxFactory.List
                                                (
                                                    methods
                                                )
                                            )
                                            .WithModifiers(
                                                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                    ))
                        }
                    )
                );

            newClass = newClass.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(System))));

            return newClass.NormalizeWhitespace().GetText();
        }
    }
}