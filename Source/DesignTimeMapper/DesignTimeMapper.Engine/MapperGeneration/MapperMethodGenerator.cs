using System;
using System.Collections.Generic;
using System.Linq;
using DesignTimeMapper.Attributes;
using DesignTimeMapper.Engine.Extensions;
using DesignTimeMapper.Engine.Interface;
using DesignTimeMapper.Engine.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.MapperGeneration
{
    public class MapperMethodGenerator : IMapperMethodGenerator
    {
        public IList<MethodWithUsings> CreateMapperMethods(Compilation compilation)
        {
            var methodDeclarationSyntaxs = new List<MethodWithUsings>();

            foreach (var classToMapToTypeSymbol in GetClassesInCompilation(compilation))
                foreach (
                    var attributeData in
                        classToMapToTypeSymbol.GetAttributes().Where(a => a.AttributeClass.Name == nameof(MapFromAttribute)))
                {
                    var type = attributeData.ConstructorArguments[0];
                    foreach (var typedConstant in type.Values)
                    {
                        var classToMapFromTypeSymbol = typedConstant.Value as INamedTypeSymbol;

                        if (classToMapFromTypeSymbol != null)
                        {
                            var methods = CreateMapperMethods(classToMapToTypeSymbol, classToMapFromTypeSymbol);
                            methodDeclarationSyntaxs.AddRange(methods);
                        }
                    }
                }

            return methodDeclarationSyntaxs;
        }

        internal IEnumerable<INamedTypeSymbol> GetClassesInCompilation(Compilation compilation)
        {
            foreach (
                var result in
                    compilation.SyntaxTrees
                        .Select(syntaxTree => compilation.GetSemanticModel(syntaxTree))
                        .Select(
                            semanticModel =>
                                semanticModel.SyntaxTree.GetRoot()
                                    .DescendantNodesAndSelf()
                                    .OfType<ClassDeclarationSyntax>()
                                    .Select(
                                        propertyDeclarationSyntax =>
                                            semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax))))
                foreach (var namedTypeSymbol in result)
                    yield return namedTypeSymbol;
        }

        private IEnumerable<MethodWithUsings> CreateMapperMethods(INamedTypeSymbol classToMapToTypeSymbol,
            INamedTypeSymbol classToMapFromTypeSymbol)
        {
            var inputArgName = classToMapFromTypeSymbol.Name.ToCamelCase();
            string classToMapFromName;
            if (classToMapToTypeSymbol.ContainingNamespace.GetFullMetadataName() ==
                classToMapFromTypeSymbol.ContainingNamespace.GetFullMetadataName())
                classToMapFromName = classToMapFromTypeSymbol.Name;
            else
                classToMapFromName = classToMapFromTypeSymbol.GetFullMetadataName();

            var classToMapToSymbols =
                classToMapToTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();
            var classToMapFromSymbols =
                classToMapFromTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();

            //TODO handle case where expression syntaxes are empty
            var classToMapToName = classToMapToTypeSymbol.GetFullMetadataName();
            //var mapToMethodDeclaration = CreateMethodDeclaration(classToMapToTypeSymbol, classToMapToName, inputArgName, classToMapFromName,
            //                                                        GetAssignmentExpressionSyntaxs(classToMapToSymbols, classToMapFromSymbols, inputArgName, new List<IPropertySymbol>()));
            //yield return new MethodWithUsings
            //{
            //    Method = mapToMethodDeclaration,
            //    Usings = new List<INamespaceSymbol>()
            //};

            GetMatchingPropertyTree(classToMapFromSymbols, classToMapToSymbols, inputArgName);

            var mapFromMethodDeclaration = CreateMethodDeclaration(classToMapFromTypeSymbol, classToMapFromName, inputArgName, classToMapToName,
                                                                    GetAssignmentExpressionSyntaxs(classToMapFromSymbols, classToMapToSymbols, inputArgName, new List<IPropertySymbol>()));
            yield return new MethodWithUsings
            {
                Method = mapFromMethodDeclaration,
                Usings = new List<INamespaceSymbol>()
            };
        }
        
        private static MethodDeclarationSyntax CreateMethodDeclaration(INamedTypeSymbol classToMapToTypeSymbol,
            string classToMapToName, string inputArgName, string classToMapFromName, IEnumerable<AssignmentExpressionSyntax> expressionSyntaxs)
        {
            var methodDeclaration = SyntaxFactory.MethodDeclaration
                (
                    SyntaxFactory.IdentifierName(classToMapToName),
                    SyntaxFactory.Identifier("MapTo" + classToMapToTypeSymbol.Name)
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
                            SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.Parameter
                                        (
                                            SyntaxFactory.Identifier(inputArgName)
                                        )
                                        .WithType
                                        (
                                            SyntaxFactory.IdentifierName(classToMapFromName)
                                        )
                                        .WithModifiers
                                        (
                                            SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.ThisKeyword))
                                        )
                                }
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
                                                    SyntaxFactory.IdentifierName(classToMapToName)
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
                                                                expressionSyntaxs
                                                                )
                                                        )
                                                )
                                        )
                                )
                        )
                );
            return methodDeclaration;
        }

        private TreeNode<MatchingProperty> GetMatchingPropertyTree(IEnumerable<IPropertySymbol> classToMapFromSymbols, IEnumerable<IPropertySymbol> classToMapToSymbols, string inputArgName)
        {
            return GetMatchingPropertyTree(classToMapFromSymbols, classToMapToSymbols, inputArgName,
                new TreeNode<MatchingProperty>(new MatchingProperty()));
        }


        private TreeNode<MatchingProperty> GetMatchingPropertyTree(IEnumerable<IPropertySymbol> classToMapFromSymbols, IEnumerable<IPropertySymbol> classToMapToSymbols, string inputArgName, TreeNode<MatchingProperty> parent)
        {
            foreach (var classToMapToSymbol in classToMapToSymbols)
            {
                var potentialName = string.Empty;
                parent.TraverseAncestors(s => potentialName.Insert(0, s.ClassToMapToProperty.Name));
                potentialName += classToMapToSymbol.Name;

                var matchingSymbols = classToMapFromSymbols.Where(s => s.Name == potentialName);

                //if (matchingSymbolName != null)
                //{
                //    var actualPropertyName = string.Join(".", parents.Select(p => p.Name).Concat(new[] { symbol.Name }));
                //    yield return SyntaxFactory.AssignmentExpression
                //    (
                //        SyntaxKind
                //            .SimpleAssignmentExpression,
                //        SyntaxFactory.IdentifierName(actualPropertyName),
                //        SyntaxFactory.MemberAccessExpression
                //        (
                //            SyntaxKind
                //                .SimpleMemberAccessExpression,
                //            SyntaxFactory.IdentifierName(inputArgName),
                //            SyntaxFactory.IdentifierName(string.Join(".", matchingSymbolName))
                //        )
                //    );
                //}
                //else
                //{
                //}
            }
            throw new NotImplementedException();
        }

        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentExpressionSyntaxs(
            IEnumerable<IPropertySymbol> classToMapToSymbols, IEnumerable<IPropertySymbol> classToMapFromSymbols,
            string inputArgName, IEnumerable<IPropertySymbol> parents)
        {
            foreach (var symbol in classToMapToSymbols)
            {
                if(symbol.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute))) continue;

                //TODO optimise this
                var potentialName = string.Join("", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
                var matchingSymbolName = GetMatchingSymbolName(classToMapFromSymbols, potentialName, symbol.Type, new List<string>());

                if (matchingSymbolName != null)
                {
                    var actualPropertyName = string.Join(".", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
                    yield return SyntaxFactory.AssignmentExpression
                    (
                        SyntaxKind
                            .SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(actualPropertyName),
                        SyntaxFactory.MemberAccessExpression
                        (
                            SyntaxKind
                                .SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(inputArgName),
                            SyntaxFactory.IdentifierName(string.Join(".", matchingSymbolName))
                        )
                    );
                }
                else
                {
                    var separatedSyntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
                    foreach (var s in classToMapFromSymbols)
                    {
                        if(s.Name == potentialName &&
                            s.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute))) continue;

                        if (s.Name.Contains(potentialName))
                        {
                            separatedSyntaxList = separatedSyntaxList.Add(SyntaxFactory.AssignmentExpression
                          (
                              SyntaxKind.SimpleAssignmentExpression,
                              SyntaxFactory.IdentifierName(s.Name.Replace(potentialName, "")),
                              SyntaxFactory.MemberAccessExpression
                              (
                                  SyntaxKind
                                      .SimpleMemberAccessExpression,
                                  SyntaxFactory.IdentifierName(inputArgName),
                                  SyntaxFactory.IdentifierName(s.Name)
                              )
                          ));
                        }
                    }

                    yield return SyntaxFactory.AssignmentExpression
                    (
                        SyntaxKind
                            .SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(symbol.Name),
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName(symbol.Type.GetFullMetadataName()),
                            SyntaxFactory.ArgumentList(), SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, separatedSyntaxList)));
                }
            }
        }

        private static List<string> GetMatchingSymbolName(IEnumerable<IPropertySymbol> classToMapFromSymbols, string potentialName, ITypeSymbol type, List<string> parentNames)
        {
            var matching = classToMapFromSymbols.FirstOrDefault(s => s.Name == potentialName && s.Type == type && !s.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute)));

            if (matching != null) return new List<string> { matching.Name };


            foreach (var classToMapFromSymbol in classToMapFromSymbols)
            {
                var members = classToMapFromSymbol.Type.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Property);
                var names = new List<string>();
                names.AddRange(parentNames);
                names.Add(classToMapFromSymbol.Name);
                var matchingName  =
                    GetMatchingSymbolName(
                        members
                            .Cast<IPropertySymbol>(), potentialName.Replace(classToMapFromSymbol.Name, ""), type, names);
                if (matchingName != null)
                {
                    names.AddRange(matchingName);
                    return names;
                }
            }

            return null;
        }

        private static void A(IEnumerable<IPropertySymbol> classToMapToSymbols, IEnumerable<IPropertySymbol> classToMapFromSymbols,
            string inputArgName)
        {
            
        }
    }
}