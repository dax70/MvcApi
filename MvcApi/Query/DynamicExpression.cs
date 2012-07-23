namespace MvcApi.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;

    internal static class DynamicExpression
    {
        private static readonly Type[] funcTypes = new Type[] { typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>) };

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification="Arguments are provided internally by the parser's ParserLambda methods.")]
        public static Type GetFuncType(params Type[] typeArgs)
        {
            if (((typeArgs == null) || (typeArgs.Length < 1)) || (typeArgs.Length > 5))
            {
                throw new ArgumentException();
            }
            return funcTypes[typeArgs.Length - 1].MakeGenericType(typeArgs);
        }

        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
        {
            int index = (parameters == null) ? 0 : parameters.Length;
            Type[] typeArgs = new Type[index + 1];
            for (int i = 0; i < index; i++)
            {
                typeArgs[i] = parameters[i].Type;
            }
            typeArgs[index] = body.Type;
            return Expression.Lambda(GetFuncType(typeArgs), body, parameters);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, QueryResolver queryResolver)
        {
            ExpressionParser parser = new ExpressionParser(parameters, expression, queryResolver);
            return Lambda(parser.Parse(resultType), parameters);
        }

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, QueryResolver queryResolver)
        {
            return ParseLambda(new ParameterExpression[] { Expression.Parameter(itType, "") }, resultType, expression, queryResolver);
        }
    }
}

