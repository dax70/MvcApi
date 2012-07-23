namespace MvcApi.Query
{
    using System;
    using System.Linq.Expressions;

    internal class DynamicOrdering
    {
        public bool Ascending;
        public Expression Selector;
    }
}

