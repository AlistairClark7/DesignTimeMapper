using Microsoft.CodeAnalysis;

namespace DesignTimeMapper.Engine.Model
{
    public class MatchingPropertyTree
    {
        public MapTreeNode<IPropertySymbol> MapFromTree { get; set; }
        public MapTreeNode<IPropertySymbol> MapToTree { get; set; }
    }
}