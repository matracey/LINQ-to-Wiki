using System;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit;
using LinqToWiki.Internals;
using LinqToWiki.Parameters;

namespace LinqToWiki.Expressions
{
    /// <summary>
    /// Parses various expression kinds and returns them as a <see cref="QueryParameters{TSource,TResult}"/>.
    /// </summary>
    internal static class ExpressionParser
    {
        /// <summary>
        /// Parses a <c>where</c> expression.
        /// </summary>
        /// <param name="expression">Expression to parse.</param>
        /// <param name="previousParameters">Previous parameters, whose values should be included in the result.</param>
        public static QueryParameters<TSource, TResult> ParseWhere<TSource, TResult, TWhere>(
            Expression<Func<TWhere, bool>> expression, QueryParameters<TSource, TResult> previousParameters)
        {
            var body = EnumFixer.Fix(PartialEvaluator.Eval(expression.Body));

            return ParseWhereSubexpression(body, previousParameters);
        }

        /// <summary>
        /// Parses a single expression from <c>where</c>. If necessary, calls itself recursively.
        /// </summary>
        private static QueryParameters<TSource, TResult> ParseWhereSubexpression<TSource, TResult>(
            Expression body, QueryParameters<TSource, TResult> previousParameters)
        {
            switch (body)
            {
                case MemberExpression memberExpression:
                    return AddValue(previousParameters, memberExpression, true);
                case UnaryExpression unaryExpression:
                    {
                        return unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Operand is MemberExpression memberAccess
                            ? AddValue(previousParameters, memberAccess, false)
                            : throw new ArgumentException($"Unknown type of unary expression: {unaryExpression}.");
                    }
            }


            if (!(body is BinaryExpression binaryExpression))
            {
                throw new ArgumentException($"Unknown type of expression: {body}.");
            }

            if (binaryExpression.NodeType == ExpressionType.Equal)
            {
                return AddValue(previousParameters, ParseWhereEqualExpression(binaryExpression));
            }

            if (binaryExpression.NodeType != ExpressionType.AndAlso && binaryExpression.NodeType != ExpressionType.And)
            {
                throw new ArgumentException($"Unknown type of binary expression: {binaryExpression}.");
            }

            var afterLeft = ParseWhereSubexpression(binaryExpression.Left, previousParameters);
            return ParseWhereSubexpression(binaryExpression.Right, afterLeft);
        }

        /// <summary>
        /// Adds single value to QueryParameters based on a property expression and a unformatted value from a <c>Tuple</c>.
        /// </summary>
        private static QueryParameters<TSource, TResult> AddValue<TSource, TResult>(
            QueryParameters<TSource, TResult> previousParameters, Tuple<MemberExpression, object> propertyValue)
        {
            return AddValue(previousParameters, propertyValue.Item1, propertyValue.Item2);
        }

        /// <summary>
        /// Adds single value to QueryParameters based on a property expression and a unformatted value.
        /// </summary>
        private static QueryParameters<TSource, TResult> AddValue<TSource, TResult>(
            QueryParameters<TSource, TResult> previousParameters, MemberExpression memberExpression, object value)
        {
            return previousParameters.AddSingleValue(
                ReversePropertyName(memberExpression.Member.Name.ToLowerInvariant()),
                QueryRepresentation.ToQueryStringDynamic(value));
        }

        /// <summary>
        /// Parses a part of <c>where</c> expression that contains <c>==</c>.
        /// </summary>
        /// <param name="expression">Subexpression to parse.</param>
        private static Tuple<MemberExpression, object> ParseWhereEqualExpression(BinaryExpression expression)
        {
            var result = ParsePropertyEqualsConstantExpression(expression)
                         ?? ParsePropertyEqualsConstantExpression(expression.Switch());

            return result ?? throw new ArgumentException($"Could not parse expression: {expression}.");
        }

        /// <summary>
        /// Parses an expression that contains <c>==</c> and parameters in the “correct” order.
        /// </summary>
        private static Tuple<MemberExpression, object> ParsePropertyEqualsConstantExpression(BinaryExpression expression)
        {
            if (!(expression.Left is MemberExpression memberAccess))
            {
                return null;
            }

            if (!(memberAccess.Expression is ParameterExpression))
            {
                return null;
            }

            if (!(expression.Right is ConstantExpression valueExpression))
            {
                return null;
            }

            var value = valueExpression.Value;

            return Tuple.Create(memberAccess, value);
        }

        /// <summary>
        /// Reverses property name into the form used by the API.
        /// </summary>
        public static string ReversePropertyName(string propertyName)
        {
            if (propertyName == "value")
            {
                return "*";
            }

            return propertyName == "defaultvalue" ? "default" : propertyName;
        }

        /// <summary>
        /// Parses an <c>orderby</c> expression.
        /// </summary>
        /// <param name="expression">Expression to parse.</param>
        /// <param name="previousParameters">Previous parameters, whose values should be included in the result.</param>
        /// <param name="ascending">Should the ordering be ascending?</param>
        public static QueryParameters<TSource, TResult> ParseOrderBy<TSource, TResult, TOrderBy, TKey>(
            Expression<Func<TOrderBy, TKey>> expression,
            QueryParameters<TSource, TResult> previousParameters,
            bool ascending)
        {
            string memberName;
            if (!(expression.Body is ParameterExpression))
            {
                if (expression.Body is MemberExpression memberAccess)
                {
                    if (!(memberAccess.Expression is ParameterExpression))
                    {
                        throw new ArgumentException();
                    }

                    memberName = memberAccess.Member.Name.ToLowerInvariant();
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else
            {
                memberName = null;
            }

            return previousParameters.WithSort(memberName, ascending);
        }

        /// <summary>
        /// Parses a <c>select</c> expression.
        /// </summary>
        /// <param name="expression">Expression to parse.</param>
        /// <param name="previousParameters">Previous parameters, whose values should be included in the result.</param>
        public static QueryParameters<TSource, TResult> ParseSelect<TSource, TResult>(
            Expression<Func<TSource, TResult>> expression, QueryParameters<TSource, TSource> previousParameters)
        {
            var parameter = expression.Parameters.Single();

            var gatherer = new UsedPropertiesGatherer();

            gatherer.Gather(expression.Body, parameter);

            var usedProperties = gatherer.UsedDirectly ? null : gatherer.UsedProperties.Select(p => p.ToLowerInvariant());

            return previousParameters.WithSelect(usedProperties, expression.Compile());
        }

        /// <summary>
        /// Parses a <c>select</c> expression that looks like an identity (based on it type).
        /// </summary>
        /// <param name="expression">Expression to parse.</param>
        /// <param name="previousParameters">Previous parameters, whose values should be included in the result.</param>
        public static QueryParameters<T, T> ParseIdentitySelect<T>(Expression<Func<T, T>> expression, QueryParameters<T, T> previousParameters)
        {
            var parameter = expression.Parameters.Single();

            var body = expression.Body as ParameterExpression;

            return parameter != body
                ? throw new InvalidOperationException($"Select expression with the return type of '{expression.Body.Type}' has to be identity.")
                : previousParameters;
        }
    }
}