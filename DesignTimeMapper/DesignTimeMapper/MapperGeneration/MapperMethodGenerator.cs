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

namespace DesignTimeMapper.MapperGeneration
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

        private MethodWithUsings CreateMapperMethod(INamedTypeSymbol classToMapToTypeSymbol, INamedTypeSymbol classToMapFromTypeSymbol)
        {
            var inputArgName = classToMapFromTypeSymbol.Name.ToCamelCase();
            string classToMapFromName;
            if (classToMapToTypeSymbol.ContainingNamespace.GetFullMetadataName() == classToMapFromTypeSymbol.ContainingNamespace.GetFullMetadataName())
                classToMapFromName = classToMapFromTypeSymbol.Name;
            else
                 classToMapFromName = classToMapFromTypeSymbol.GetFullMetadataName();
            

            List<MemberDeclarationSyntax> classToMapToProperties = new List<MemberDeclarationSyntax>();
            foreach (var declaringSyntaxReference in classToMapToTypeSymbol.DeclaringSyntaxReferences)
            {
                classToMapToProperties.AddRange(declaringSyntaxReference.GetSyntax().DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>());
            }
            IEnumerable<IPropertySymbol> classToMapToSymbols = classToMapToTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();
            IEnumerable<IPropertySymbol> classToMapFromSymbols = classToMapFromTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(classToMapToSymbols, classToMapFromSymbols, inputArgName, new IPropertySymbol[0]);
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

        
        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentExpressionSyntaxs(IEnumerable<IPropertySymbol> classToMapToSymbols, IEnumerable<IPropertySymbol> classToMapFromSymbols, string inputArgName, IEnumerable<IPropertySymbol> parents)
        {
            foreach (var symbol in classToMapFromSymbols)
            {
                //TODO optimise this
                var potentialName = string.Join("", parents.Select(p => p.Name).Concat(new [] { symbol.Name} ));
                var matchingSymbol = classToMapToSymbols.FirstOrDefault(s => s.Name == potentialName && !s.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute)));

                if (matchingSymbol != null)
                {
                    var actualName = string.Join(".", parents.Select(p => p.Name).Concat(new[] { symbol.Name }));
                    yield return SyntaxFactory.AssignmentExpression
                        (
                            SyntaxKind
                                .SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(matchingSymbol.Name),
                            SyntaxFactory.MemberAccessExpression
                                (
                                    SyntaxKind
                                        .SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(inputArgName),
                                    SyntaxFactory.IdentifierName(actualName)
                                )
                        );
                }

                foreach (var v in GetAssignmentExpressionSyntaxs(classToMapToSymbols, symbol.Type.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>(), inputArgName, parents.Concat(new[] {symbol})))
                {
                    yield return v;
                }
            }
        }

        private static IEnumerable<string> SplitStringOnCapital(string str)
        {
            int lastStart = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if(i == 0) continue;

                var c = str[i];

                if (i == str.Length - 1)
                {
                    yield return str.Substring(lastStart);
                }

                if (char.IsUpper(c))
                {
                    var s = str.Substring(lastStart, i - lastStart);
                    lastStart = i;
                    yield return s;
                }
            }
        }
    }
}