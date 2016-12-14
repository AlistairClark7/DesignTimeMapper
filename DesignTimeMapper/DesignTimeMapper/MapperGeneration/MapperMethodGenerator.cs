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
            List<ISymbol> classToMapFromSymbols = classToMapFromTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).ToList();
            var assignmentExpressionSyntaxs = GetAssignmentExpressionSyntaxs(classToMapToProperties, classToMapFromSymbols, inputArgName);
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

        
        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentExpressionSyntaxs(List<MemberDeclarationSyntax> properties, List<ISymbol> classToMapFromSymbols, string inputArgName)
        {
            foreach (var memberDeclarationSyntax in properties)
            {
                var prop = (PropertyDeclarationSyntax)memberDeclarationSyntax;
                var name = prop.Identifier.ToString();

                string classToMapFromPropertyName;

                classToMapFromPropertyName = GetClassToMapFromQualifiedPropertyName(classToMapFromSymbols, name);

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
                        SyntaxFactory.IdentifierName(classToMapFromPropertyName)
                    )
                );
            }
        }

        private static string GetClassToMapFromQualifiedPropertyName(List<ISymbol> classToMapFromSymbols, string propertyToMapToName)
        {
            var simpleProperty = classToMapFromSymbols.FirstOrDefault(s => s.Name == propertyToMapToName);

            if (simpleProperty != null)
            {
                return simpleProperty.Name;
            }

            return GetComplexPropertyName(classToMapFromSymbols, propertyToMapToName);
        }

        private static string GetComplexPropertyName(List<ISymbol> classToMapFromSymbols, string propertyToMapToName)
        {
            //TODO: make recursive and search remaining split names to see if they match eg. [.,..,...,....] 
            var splitNames = SplitStringOnCapital(propertyToMapToName).ToList();

            for (int i = 0; i < splitNames.Count; i++)
            {
                var splitName = splitNames[i];

                var property = classToMapFromSymbols.FirstOrDefault(s => s.Name == splitName) as IPropertySymbol;

                if (property == null) continue;

                var isLast = splitNames.Count - i - 2 == 0;

                var type = property.Type;
                var subPropertyName = string.Concat(splitNames.GetRange(i + 1, splitNames.Count - i - 1));
                var subProperty = type.GetMembers().Where(m => m.Kind == SymbolKind.Property).FirstOrDefault(s => s.Name == subPropertyName);
                if (subProperty == null) continue;

                if (isLast)
                {
                    return string.Join(".", splitNames);
                }
            }

            return propertyToMapToName;
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