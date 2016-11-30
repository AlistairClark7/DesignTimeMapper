using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DesignTimeMapper.Engine
{
    public class ClassMapper
    {
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
            var mapperMethod = CreateMapperMethod(nsMember, properties, newClassName);
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

        private MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass, List<MemberDeclarationSyntax> properties, string newClassName)
        {
            var c = originalClass as ClassDeclarationSyntax;
            if (c == null)
                throw new NotImplementedException();

            var originalClassName = c.Identifier.ToString();
            
            var inputArgName = originalClassName.ToCamelCase();
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(properties, inputArgName);
            return MethodDeclaration
                (
                    IdentifierName(newClassName),
                    Identifier("Create")
                )
                .WithModifiers
                (
                    TokenList
                    (Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword))
                )
                .WithParameterList
                (
                    ParameterList
                    (
                        SingletonSeparatedList
                        (
                            Parameter
                                (
                                    Identifier(inputArgName)
                                )
                                .WithType
                                (
                                    IdentifierName(originalClassName)
                                )
                        )
                    )
                )
                .WithBody
                (
                    Block
                    (
                        SingletonList<StatementSyntax>
                        (
                            ReturnStatement
                            (
                                ObjectCreationExpression
                                    (
                                        IdentifierName(newClassName)
                                    )
                                    .WithArgumentList
                                    (
                                        ArgumentList()
                                    )
                                    .WithInitializer
                                    (
                                        InitializerExpression
                                        (
                                            SyntaxKind.ObjectInitializerExpression,
                                            SeparatedList<ExpressionSyntax>
                                            (
                                                assignmentExpressionSyntaxs
                                            )
                                        )
                                    )
                            )
                        )
                    )
                );
        }

        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentExpressionSyntaxs(List<MemberDeclarationSyntax> properties, string inputArgName)
        {
            foreach (var memberDeclarationSyntax in properties)
            {
                var prop = (PropertyDeclarationSyntax) memberDeclarationSyntax;
                var name = prop.Identifier.ToString();

                yield return AssignmentExpression
                (
                    SyntaxKind
                        .SimpleAssignmentExpression,
                    IdentifierName(name),
                    MemberAccessExpression
                    (
                        SyntaxKind
                            .SimpleMemberAccessExpression,
                        IdentifierName(inputArgName),
                        IdentifierName(name)
                    )
                );
            }
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