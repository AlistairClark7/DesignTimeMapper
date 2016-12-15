using System.Text;
using Microsoft.CodeAnalysis;

namespace DesignTimeMapper.Engine.Extensions
{
    public static class RoslynExtensions
    {

        public static string GetFullMetadataName(this ISymbol symbol)
        {
            var sb = new StringBuilder(symbol.MetadataName);

            var last = symbol;
            symbol = symbol.ContainingSymbol;
            while (!IsRootNamespace(symbol))
            {
                if (symbol is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }
                sb.Insert(0, symbol.MetadataName);
                symbol = symbol.ContainingSymbol;
            }

            return sb.ToString();
        }


        private static bool IsRootNamespace(ISymbol s)
        {
            return s is INamespaceSymbol && ((INamespaceSymbol)s).IsGlobalNamespace;
        }
    }
}