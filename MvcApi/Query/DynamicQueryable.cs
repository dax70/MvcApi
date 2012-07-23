namespace MvcApi.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;

    internal static class DynamicQueryable
    {
        public static IQueryable OrderBy(this IQueryable source, string ordering, QueryResolver queryResolver)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (ordering == null)
            {
                throw new ArgumentNullException("ordering");
            }
            ParameterExpression[] parameters = new ParameterExpression[] { Expression.Parameter(source.ElementType, "") };
            IEnumerable<DynamicOrdering> enumerable = new ExpressionParser(parameters, ordering, queryResolver).ParseOrdering();
            Expression expression = source.Expression;
            string str = "OrderBy";
            string str2 = "OrderByDescending";
            foreach (DynamicOrdering ordering2 in enumerable)
            {
                expression = Expression.Call(typeof(Queryable), ordering2.Ascending ? str : str2, new Type[] { source.ElementType, ordering2.Selector.Type }, new Expression[] { expression, Expression.Quote(DynamicExpression.Lambda(ordering2.Selector, parameters)) });
                str = "ThenBy";
                str2 = "ThenByDescending";
            }
            return source.Provider.CreateQuery(expression);
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), "Skip", new Type[] { source.ElementType }, new Expression[] { source.Expression, Expression.Constant(count) }));
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), "Take", new Type[] { source.ElementType }, new Expression[] { source.Expression, Expression.Constant(count) }));
        }

        public static IQueryable Where(this IQueryable source, string predicate, QueryResolver queryResolver)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            LambdaExpression expression = DynamicExpression.ParseLambda(source.ElementType, typeof(bool), predicate, queryResolver);
            return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), "Where", new Type[] { source.ElementType }, new Expression[] { source.Expression, Expression.Quote(expression) }));
        }
    }
}

