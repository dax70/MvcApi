namespace MvcApi.Query
{
    using System;
    using System.Linq.Expressions;

    internal abstract class QueryResolver
    {
        protected QueryResolver()
        {
        }

        public abstract MemberExpression ResolveMember(Type type, string member, Expression instance);
    }
}

