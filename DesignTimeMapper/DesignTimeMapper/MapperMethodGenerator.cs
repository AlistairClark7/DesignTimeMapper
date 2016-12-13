using System;
using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Attributes;
using DesignTimeMapper.Extensions;
using DesignTimeMapper.Interface;
using DesignTimeMapper.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper
{
    public class MapperMethodGenerator : IMapperMethodGenerator
    {
        public IList<MethodWithUsings> CreateMapperMethods(Compilation compilation)
        {
            var methodDeclarationSyntaxs = new List<MethodWithUsings>();
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
                                var withUsings = CreateMapperMethod(typeMember, attributeTypeSymbol);
                                methodDeclarationSyntaxs.Add(withUsings);
                            }
                        }
                    }
                }
            }

            return methodDeclarationSyntaxs;
        }

        private MethodWithUsings CreateMapperMethod(INamedTypeSymbol classToMapToTypeSymbol, INamedTypeSymbol attributeTypeSymbol)
        {
            var inputArgName = attributeTypeSymbol.Name.ToCamelCase();
            string classToMapFromName;
            if (classToMapToTypeSymbol.ContainingNamespace.GetFullMetadataName() == attributeTypeSymbol.ContainingNamespace.GetFullMetadataName())
                classToMapFromName = attributeTypeSymbol.Name;
            else
                 classToMapFromName = attributeTypeSymbol.GetFullMetadataName();
            

            List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
            foreach (var declaringSyntaxReference in classToMapToTypeSymbol.DeclaringSyntaxReferences)
            {
                properties.AddRange(declaringSyntaxReference.GetSyntax().DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>());
            }
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(properties, inputArgName);
            var methodDeclaration = SyntaxFactory.MethodDeclaration
                (
                    SyntaxFactory.IdentifierName(classToMapToTypeSymbol.Name),
                    SyntaxFactory.Identifier("MapFrom")
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
                                    SyntaxFactory.IdentifierName(classToMapFromName)
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
                                        SyntaxFactory.IdentifierName(classToMapToTypeSymbol.Name)
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
            
            var withUsings = new MethodWithUsings
            {
                Method = methodDeclaration,
                Usings = new List<INamespaceSymbol>
                {
                    classToMapToTypeSymbol.ContainingNamespace
                }
            };

            return withUsings;
        }

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