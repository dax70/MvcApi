namespace MvcApi.Query
{
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using MvcApi.Properties;

    public class QueryValidator
    {
        private static QueryValidator _instance = new QueryValidator();

        protected QueryValidator()
        {
        }

        public virtual void Validate(IQueryable query)
        {
            //QueryValidationVisitor.Validate(query);
        }

        public static QueryValidator Instance
        {
            get { return _instance; }
        }

        private class QueryValidationVisitor : ExpressionVisitor
        {
            private static bool HasAttribute<T>(object[] attribs)
            {
                return ((attribs.Length != 0) && attribs.OfType<T>().Any<T>());
            }

            private static bool IsVisible(MemberInfo member)
            {
                return true;
                //bool inherit = true;
                //object[] customAttributes = member.GetCustomAttributes(inherit);
                //bool flag2 = true;
                //return (((!HasAttribute<DataContractAttribute>(member.ReflectedType.GetCustomAttributes(flag2)) || HasAttribute<DataMemberAttribute>(customAttributes)) && (!HasAttribute<XmlIgnoreAttribute>(customAttributes) && !HasAttribute<IgnoreDataMemberAttribute>(customAttributes))) && !HasAttribute<NonSerializedAttribute>(customAttributes));
            }

            public static void Validate(IQueryable query)
            {
                new QueryValidator.QueryValidationVisitor().Visit(query.Expression);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (!IsVisible(node.Member))
                {
                    throw Error.InvalidOperation(SR.UnknownPropertyOrField, new object[] { node.Member.Name, node.Member.DeclaringType.Name });
                }
                return base.VisitMember(node);
            }
        }
    }
}

