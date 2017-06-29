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
        public IList<MemberDeclarationSyntax> CreateMapperMethods(Compilation compilation)
        {
            var methodDeclarationSyntaxs = new List<MemberDeclarationSyntax>();

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

        private IEnumerable<MemberDeclarationSyntax> CreateMapperMethods(INamedTypeSymbol classToMapToTypeSymbol,
            INamedTypeSymbol classToMapFromTypeSymbol)
        {
            var inputArgName = classToMapFromTypeSymbol.Name.ToCamelCase();
            string classToMapFromName = classToMapFromTypeSymbol.GetFullMetadataName();

            var classToMapToSymbols =
                classToMapToTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();
            var classToMapFromSymbols =
                classToMapFromTypeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();

            //TODO handle case where expression syntaxes are empty
            var classToMapToName = classToMapToTypeSymbol.GetFullMetadataName();
            
            var tree = GetMatchingPropertyTree(classToMapFromSymbols, classToMapToSymbols, inputArgName);

            var mapToMethodDeclaration = CreateMethodDeclaration(classToMapToTypeSymbol, classToMapToName, inputArgName, classToMapFromName,
                GetAssignmentExpressionSyntaxs(tree.MapToTree, inputArgName));
            yield return  mapToMethodDeclaration;

            var mapFromMethodDeclaration = CreateMethodDeclaration(classToMapFromTypeSymbol, classToMapFromName, inputArgName, classToMapToName,
                GetAssignmentExpressionSyntaxs(tree.MapFromTree, inputArgName));
            yield return mapFromMethodDeclaration;
        }
        
        private static MethodDeclarationSyntax CreateMethodDeclaration(INamedTypeSymbol classToMapToTypeSymbol,
            string classToMapToName, string inputArgName, string classToMapFromName, IEnumerable<ExpressionSyntax> expressionSyntaxs)
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
                    SyntaxFactory.Block(SyntaxFactory.TryStatement(ReturnBlock(classToMapToName, expressionSyntaxs), SyntaxFactory.SingletonList(SyntaxFactory.CatchClause(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(nameof(NullReferenceException))), null, DefaultReturnBlock(classToMapToName))), null))
                );
            return methodDeclaration;
        }

        private static BlockSyntax ReturnBlock(string classToMapToName, IEnumerable<ExpressionSyntax> expressionSyntaxs)
        {
            return SyntaxFactory.Block
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
            );
        }
        private static BlockSyntax DefaultReturnBlock(string classToMapToName)
        {
            return SyntaxFactory.Block
            (
                SyntaxFactory.SingletonList<StatementSyntax>
                (
                    SyntaxFactory.ReturnStatement
                    (
                        SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(classToMapToName))
                    )
                )
            );
        }

        private MatchingPropertyTree GetMatchingPropertyTree(IEnumerable<IPropertySymbol> classToMapFromSymbols, IEnumerable<IPropertySymbol> classToMapToSymbols, string inputArgName)
        {
            var mapToTree = new MapTreeNode<IPropertySymbol>(null);
            var mapFromTree = new MapTreeNode<IPropertySymbol>(null);

            BuildMatchingPropertyTree(classToMapToSymbols, mapToTree, classToMapFromSymbols, mapFromTree);
            
            var matchingPropertyTree = new MatchingPropertyTree
            {
                MapFromTree = mapFromTree,
                MapToTree = mapToTree
            };
            return matchingPropertyTree;
        }


        private void BuildMatchingPropertyTree(IEnumerable<IPropertySymbol> classToMapToSymbols, MapTreeNode<IPropertySymbol> mapToNode, IEnumerable<IPropertySymbol> classToMapFromSymbols, MapTreeNode<IPropertySymbol> mapFromNode)
        {
            foreach (var classToMapToSymbol in classToMapToSymbols)
            {
                var mapToChild = mapToNode.AddOrGetChild(classToMapToSymbol);
                if(mapToChild.MapsTo != null) continue;

                string potentialName = string.Empty;
                mapToChild.TraverseAncestors(s => potentialName = potentialName.Insert(0, s.Name));

                foreach (var classToMapFromSymbol in classToMapFromSymbols)
                {
                    var mapFromChild = mapFromNode.AddOrGetChild(classToMapFromSymbol);
                    if (potentialName == classToMapFromSymbol.Name)
                    {
                        mapToChild.AddMapping(mapFromChild);
                        break;
                    }
                    if(potentialName.StartsWith(classToMapFromSymbol.Name))
                    {
                        IPropertySymbol mapFromChildProperty = GetMatchingChild(mapFromChild, potentialName,
                            classToMapFromSymbol.Type.GetMembers()
                                .Where(m => m.Kind == SymbolKind.Property)
                                .Cast<IPropertySymbol>());

                        if (mapFromChildProperty != null)
                        {
                            var mapFromChildChild = mapFromChild.AddChild(mapFromChildProperty);
                            mapToChild.AddMapping(mapFromChildChild);
                            break;   
                        }
                    }
                    if (classToMapFromSymbol.Name.StartsWith(potentialName))
                    {
                        var childSymbols = classToMapToSymbol.Type.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>();
                        BuildMatchingPropertyTree(childSymbols, mapToChild, classToMapFromSymbols, mapFromNode);
                    }
                }
            }
        }

        private IPropertySymbol GetMatchingChild(MapTreeNode<IPropertySymbol> parent, string potentialName, IEnumerable<IPropertySymbol> propertySymbols)
        {
            foreach (var propertySymbol in propertySymbols)
            {
                var potentialMappedName = string.Empty;
                parent.TraverseAncestors(s => potentialMappedName = potentialMappedName.Insert(0, s.Name));
                potentialMappedName += propertySymbol.Name;

                if (potentialName == potentialMappedName)
                    return propertySymbol;
            }
            return null;
        }

        private static IEnumerable<ExpressionSyntax> GetAssignmentExpressionSyntaxs(
            MapTreeNode<IPropertySymbol> node, string inputArgName)
        {
            foreach (var child in node.Children)
            {
                if (child.Children.Any())
                {
                    var separatedSyntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
                    foreach (var assignmentExpressionSyntax in GetAssignmentExpressionSyntaxs(child, inputArgName))
                    {
                        separatedSyntaxList = separatedSyntaxList.Add(assignmentExpressionSyntax);
                    }

                    yield return SyntaxFactory.AssignmentExpression
                    (
                    SyntaxKind
                        .SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(child.Value.Name),
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(child.Value.Type.GetFullMetadataName()),
                        SyntaxFactory.ArgumentList(), SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, separatedSyntaxList)));


                }
                else if(HasValidMapping(child))
                {
                    //TODO better way of getting the name
                    var ancestors = child.MapsTo.GetAncestors();
                    var name = string.Join(".", ancestors.Select(a => a.Name)) + "." + child.MapsTo.Value.Name;
                    if (name.StartsWith(".")) name = name.Remove(0, 1);

                    yield return SyntaxFactory.AssignmentExpression
                    (
                        SyntaxKind
                            .SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(child.Value.Name),
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

        private static bool HasValidMapping(MapTreeNode<IPropertySymbol> child)
        {
            var hasMapping = child.MapsTo != null;

            if (!hasMapping) return false;

            var childHasDoNotMapAttribute = child.Value.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute));
            var mapsToHasDoNotMapAttribute = child.MapsTo.Value.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute));
            return !childHasDoNotMapAttribute &&
                   !mapsToHasDoNotMapAttribute;
        }
    }
}