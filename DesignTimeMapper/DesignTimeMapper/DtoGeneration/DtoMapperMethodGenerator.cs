using System;
using System.Collections.Generic;
using DesignTimeMapper.Extensions;
using DesignTimeMapper.Interface;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.DtoGeneration
{
    public class DtoMapperMethodGenerator : IDtoMapperMethodGenerator
    {
        public MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass, List<MemberDeclarationSyntax> properties, string newClassName)
        {
            var c = originalClass as ClassDeclarationSyntax;
            if (c == null)
                throw new NotImplementedException();

            var originalClassName = c.Identifier.ToString();

            var inputArgName = originalClassName.ToCamelCase();
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(properties, inputArgName);
            return SyntaxFactory.MethodDeclaration
                (
                    SyntaxFactory.IdentifierName(newClassName),
                    SyntaxFactory.Identifier("Create")
                )
                .WithModifiers
                (
                    SyntaxFactory.TokenList
                    (SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                )
                .WithParameterList
                (
                    SyntaxFactory.ParameterList
                    (
                        SyntaxFactory.SingletonSeparatedList
                        (
                            SyntaxFactory.Parameter
                                (
                                    SyntaxFactory.Identifier(inputArgName)
                                )
                                .WithType
                                (
                                    SyntaxFactory.IdentifierName(originalClassName)
                                )
                        )
                    )
                )
                .WithBody
                (
                    SyntaxFactory.Block
                    (
                        SyntaxFactory.SingletonList<StatementSyntax>
                        (
                            SyntaxFactory.ReturnStatement
                            (
                                SyntaxFactory.ObjectCreationExpression
                                    (
                                        SyntaxFactory.IdentifierName(newClassName)
                                    )
                                    .WithArgumentList
                                    (
                                        SyntaxFactory.ArgumentList()
                                    )
                                    .WithInitializer
                                    (
                                        SyntaxFactory.InitializerExpression
                                        (
                                            SyntaxKind.ObjectInitializerExpression,
                                            SyntaxFactory.SeparatedList<ExpressionSyntax>
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
                var prop = (PropertyDeclarationSyntax)memberDeclarationSyntax;
                var name = prop.Identifier.ToString();

                yield return SyntaxFactory.AssignmentExpression
                (
                    SyntaxKind
                        .SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(name),
                    SyntaxFactory.MemberAccessExpression
                    (
                        SyntaxKind
                            .SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(inputArgName),
                        SyntaxFactory.IdentifierName(name)
                    )
                );
            }
        }
    }
}