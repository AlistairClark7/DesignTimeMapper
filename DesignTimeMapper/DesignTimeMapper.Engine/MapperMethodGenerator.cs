using System;
using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Engine.Attributes;
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
            var methodDeclarationSyntaxs = new List<MethodDeclarationSyntax>();
            foreach (var ns in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
            {
                foreach (var namespaceMember in ns.GetNamespaceMembers())
                {
                    foreach (var typeMember in namespaceMember.GetTypeMembers())
                    {
                        foreach (var attributeData in typeMember.GetAttributes().Where(a => a.AttributeClass.Name == nameof(MapFromAttribute)))
                        {
                            var type = attributeData.ConstructorArguments[0];
                            INamedTypeSymbol attributeTypeSymbol = type.Value as INamedTypeSymbol;

                            if (attributeTypeSymbol != null)
                            {
                                methodDeclarationSyntaxs.Add(CreateMapperMethod(typeMember, attributeTypeSymbol));

                                foreach (var typeSymbolMemberName in attributeTypeSymbol.MemberNames)
                                {
                                
                                }
                            }
                        }

                        foreach (var member in typeMember.GetMembers())
                        {

                        }
                    }
                }
            }

            return methodDeclarationSyntaxs;
        }

        private MethodDeclarationSyntax CreateMapperMethod(INamedTypeSymbol classToMapToTypeSymbol, INamedTypeSymbol attributeTypeSymbol)
        {
            var inputArgName = attributeTypeSymbol.Name.ToCamelCase();
            var classToMapFromName = attributeTypeSymbol.Name;

            List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
            foreach (var declaringSyntaxReference in classToMapToTypeSymbol.DeclaringSyntaxReferences)
            {
                properties.AddRange(declaringSyntaxReference.GetSyntax().DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>());
            }
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(properties, inputArgName);
            var methodDeclaration = MethodDeclaration
                (
                    IdentifierName(classToMapToTypeSymbol.Name),
                    Identifier("MapFrom")
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
                                    IdentifierName(classToMapFromName)
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
                                        IdentifierName(classToMapToTypeSymbol.Name)
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
            
            return methodDeclaration;
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