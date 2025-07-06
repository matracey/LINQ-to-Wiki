using System.Linq.Expressions;

namespace LinqToWiki.Expressions
{
    /// <summary>
    /// Contains extension methods for <see cref="Expression"/> types.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Switches sides of a binary expression.
        /// </summary>
        public static BinaryExpression Switch(this BinaryExpression expression)
        {
            return expression == null ? null : Expression.MakeBinary(expression.NodeType, expression.Right, expression.Left);
        }
    }
}