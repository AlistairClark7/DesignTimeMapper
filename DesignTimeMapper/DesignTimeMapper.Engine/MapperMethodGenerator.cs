﻿using System;
using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Engine.Interface;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DesignTimeMapper.Engine
{
    public class MapperMethodGenerator : IMapperMethodGenerator
    {
        public IList<MethodDeclarationSyntax> CreateMapperMethods(Compilation compilation)
        {
            foreach (var ns in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
            {
                foreach (var namespaceMember in ns.GetNamespaceMembers())
                {
                    foreach (var typeMember in namespaceMember.GetTypeMembers().Where(tm => tm.GetAttributes().Any(a => a.AttributeClass.Name == "MapFromAttribute")))
                    {
                        foreach (var member in typeMember.GetMembers())
                        {

                        }
                    }
                }
            }

            return new List<MethodDeclarationSyntax>();
        }

        public MethodDeclarationSyntax CreateMapperMethod(MemberDeclarationSyntax originalClass, List<MemberDeclarationSyntax> properties, string newClassName)
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
                var prop = (PropertyDeclarationSyntax)memberDeclarationSyntax;
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
    }
}