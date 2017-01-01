using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DesignTimeMapper.Engine.Model
{
    public class MapperMethodExpression
    {
        public MapperMethodExpression(AssignmentExpressionSyntax assignmentExpressionSyntax, MapType type)
        {
            AssignmentExpressionSyntax = assignmentExpressionSyntax;
            Type = type;
        }

        public AssignmentExpressionSyntax AssignmentExpressionSyntax { get; set; }
        public MapType Type { get; set; }
    }
}