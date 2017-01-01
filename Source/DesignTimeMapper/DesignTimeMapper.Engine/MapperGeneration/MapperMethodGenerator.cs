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
            var assignmentExpressionSyntaxs = GetBothAssignmentExpressionSyntaxs(classToMapToSymbols, classToMapFromSymbols,
                inputArgName, new IPropertySymbol[0]);
            var classToMapToName = classToMapToTypeSymbol.GetFullMetadataName();
            var mapToMethodDeclaration = CreateMethodDeclaration(classToMapToTypeSymbol, classToMapToName, inputArgName, classToMapFromName,
                                                                    assignmentExpressionSyntaxs.Where(
                                                                        a => a.Type == MapType.MapTo)
                                                                        .Select(a => a.AssignmentExpressionSyntax));

            yield return new MethodWithUsings
            {
                Method = mapToMethodDeclaration,
                Usings = new List<INamespaceSymbol>()
            };

            var mapFromMethodDeclaration = CreateMethodDeclaration(classToMapFromTypeSymbol, classToMapFromName, inputArgName, classToMapToName,
                                                                    assignmentExpressionSyntaxs.Where(
                                                                        a => a.Type == MapType.MapFrom)
                                                                        .Select(a => a.AssignmentExpressionSyntax));
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

        private static IEnumerable<MapperMethodExpression> GetBothAssignmentExpressionSyntaxs(
            IEnumerable<IPropertySymbol> classToMapToSymbols, IEnumerable<IPropertySymbol> classToMapFromSymbols,
            string inputArgName, IEnumerable<IPropertySymbol> parents)
        {
            foreach (var symbol in classToMapFromSymbols)
            {
                //TODO optimise this
                var potentialName = string.Join("", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
                var matchingSymbol =
                    classToMapToSymbols.FirstOrDefault(
                        s =>
                            (s.Name == potentialName) &&
                            !s.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute)));

                if (matchingSymbol != null)
                {
                    var actualPropertyName = string.Join(".", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
                    yield return new MapperMethodExpression(SyntaxFactory.AssignmentExpression
                        (
                            SyntaxKind
                                .SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(matchingSymbol.Name),
                            SyntaxFactory.MemberAccessExpression
                                (
                                    SyntaxKind
                                        .SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(inputArgName),
                                    SyntaxFactory.IdentifierName(actualPropertyName)
                                )
                        ), MapType.MapTo);

                    if (parents != null && parents.Any())
                    {

                    }
                    else
                    {
                        yield return new MapperMethodExpression(SyntaxFactory.AssignmentExpression
                            (
                                SyntaxKind
                                    .SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(actualPropertyName),
                                SyntaxFactory.MemberAccessExpression
                                    (
                                        SyntaxKind
                                            .SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(inputArgName),
                                        SyntaxFactory.IdentifierName(matchingSymbol.Name)
                                    )
                            ), MapType.MapFrom);
                    }
                }

                foreach (
                    var v in
                        GetAssignmentExpressionSyntaxs(classToMapToSymbols,
                            symbol.Type.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>(),
                            inputArgName, parents.Concat(new[] {symbol})))
                    yield return new MapperMethodExpression(v, MapType.MapTo);
            }
        }


        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentExpressionSyntaxs(
            IEnumerable<IPropertySymbol> classToMapToSymbols, IEnumerable<IPropertySymbol> classToMapFromSymbols,
            string inputArgName, IEnumerable<IPropertySymbol> parents)
        {
            foreach (var symbol in classToMapFromSymbols)
            {
                //TODO optimise this
                var potentialName = string.Join("", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
                var matchingSymbol =
                    classToMapToSymbols.FirstOrDefault(
                        s =>
                            (s.Name == potentialName) &&
                            !s.GetAttributes().Any(a => a.AttributeClass.Name == nameof(DoNotMapAttribute)));

                if (matchingSymbol != null)
                {
                    var actualName = string.Join(".", parents.Select(p => p.Name).Concat(new[] {symbol.Name}));
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

                foreach (
                    var v in
                        GetAssignmentExpressionSyntaxs(classToMapToSymbols,
                            symbol.Type.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>(),
                            inputArgName, parents.Concat(new[] {symbol})))
                    yield return v;
            }
        }

        private static IEnumerable<string> SplitStringOnCapital(string str)
        {
            var lastStart = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (i == 0) continue;

                var c = str[i];

                if (i == str.Length - 1)
                    yield return str.Substring(lastStart);

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