namespace MvcApi.Query
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using MvcApi.Properties; 
    #endregion

    internal class ExpressionParser
    {
        private char ch;
        private static readonly Expression falseLiteral = Expression.Constant(false);
        private ParameterExpression it;
        private static Dictionary<string, object> keywords;
        private Dictionary<Expression, string> literals;
        private static readonly Expression nullLiteral = Expression.Constant(null);
        private QueryResolver queryResolver;
        private Dictionary<string, object> symbols;
        private string text;
        private int textLen;
        private int textPos;
        private Token token;
        private static readonly Expression trueLiteral = Expression.Constant(true);

        public ExpressionParser(ParameterExpression[] parameters, string expression, QueryResolver queryResolver)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (keywords == null)
            {
                keywords = CreateKeywords();
            }
            this.queryResolver = queryResolver;
            this.symbols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            this.literals = new Dictionary<Expression, string>();
            if (parameters != null)
            {
                this.ProcessParameters(parameters);
            }
            this.text = expression;
            this.textLen = this.text.Length;
            this.SetTextPos(0);
            this.NextToken();
        }

        private static void AddInterface(List<Type> types, Type type)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
                foreach (Type type2 in type.GetInterfaces())
                {
                    AddInterface(types, type2);
                }
            }
        }

        private void AddSymbol(string name, object value)
        {
            if (this.symbols.ContainsKey(name))
            {
                throw this.ParseError(string.Format(CultureInfo.CurrentCulture, SR.DuplicateIdentifier, new object[] { name }), new object[0]);
            }
            this.symbols.Add(name, value);
        }

        private void CheckAndPromoteOperand(Type signatures, string opName, ref Expression expr, int errorPos)
        {
            MethodBase base2;
            Expression[] args = new Expression[] { expr };
            if (this.FindMethod(signatures, "F", false, args, out base2) != 1)
            {
                throw ParseError(errorPos, string.Format(CultureInfo.CurrentCulture, SR.IncompatibleOperand, new object[] { opName, GetTypeName(args[0].Type) }), new object[0]);
            }
            expr = args[0];
        }

        private void CheckAndPromoteOperands(Type signatures, string opName, ref Expression left, ref Expression right, int errorPos)
        {
            MethodBase base2;
            Expression[] args = new Expression[] { left, right };
            if (this.FindMethod(signatures, "F", false, args, out base2) != 1)
            {
                throw IncompatibleOperandsError(opName, left, right, errorPos);
            }
            left = args[0];
            right = args[1];
        }

        private static int CompareConversions(Type s, Type t1, Type t2)
        {
            if (t1 != t2)
            {
                if (s == t1)
                {
                    return 1;
                }
                if (s == t2)
                {
                    return -1;
                }
                bool flag = IsCompatibleWith(t1, t2);
                bool flag2 = IsCompatibleWith(t2, t1);
                if (flag && !flag2)
                {
                    return 1;
                }
                if (flag2 && !flag)
                {
                    return -1;
                }
                if (IsSignedIntegralType(t1) && IsUnsignedIntegralType(t2))
                {
                    return 1;
                }
                if (IsSignedIntegralType(t2) && IsUnsignedIntegralType(t1))
                {
                    return -1;
                }
            }
            return 0;
        }

        private static Expression ConvertEnumExpression(Expression expr, Expression otherExpr)
        {
            Type underlyingType;
            if (!IsEnumType(expr.Type))
            {
                return expr;
            }
            if (IsNullableType(expr.Type) || ((otherExpr.NodeType == ExpressionType.Constant) && (((ConstantExpression)otherExpr).Value == null)))
            {
                underlyingType = typeof(Nullable<>).MakeGenericType(new Type[] { Enum.GetUnderlyingType(GetNonNullableType(expr.Type)) });
            }
            else
            {
                underlyingType = Enum.GetUnderlyingType(expr.Type);
            }
            return Expression.Convert(expr, underlyingType);
        }

        private static Dictionary<string, object> CreateKeywords()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            dictionary.Add("true", trueLiteral);
            dictionary.Add("false", falseLiteral);
            dictionary.Add("null", nullLiteral);
            dictionary.Add("binary", typeof(byte[]));
            dictionary.Add("X", typeof(byte[]));
            dictionary.Add("time", typeof(TimeSpan));
            dictionary.Add("datetime", typeof(DateTime));
            dictionary.Add("datetimeoffset", typeof(DateTimeOffset));
            dictionary.Add("guid", typeof(Guid));
            return dictionary;
        }

        private Expression CreateLiteral(object value, string valueAsString)
        {
            ConstantExpression key = Expression.Constant(value);
            this.literals.Add(key, valueAsString);
            return key;
        }

        private int FindBestMethod(IEnumerable<MethodBase> methods, Expression[] args, out MethodBase method)
        {
            ExpressionParser.MethodData[] applicable = (
                from m in methods
                select new ExpressionParser.MethodData
                {
                    MethodBase = m,
                    Parameters = m.GetParameters()
                } into m
                where this.IsApplicable(m, args)
                select m).ToArray<ExpressionParser.MethodData>();
            if (applicable.Length > 1)
            {
                applicable = (
                    from m in applicable
                    where applicable.All((ExpressionParser.MethodData n) => m == n || ExpressionParser.IsBetterThan(args, m, n))
                    select m).ToArray<ExpressionParser.MethodData>();
            }
            if (applicable.Length == 1)
            {
                ExpressionParser.MethodData methodData = applicable[0];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = methodData.Args[i];
                }
                method = methodData.MethodBase;
            }
            else
            {
                method = null;
            }
            return applicable.Length;
        }

        private static Type FindGenericType(Type generic, Type type)
        {
            while ((type != null) && (type != typeof(object)))
            {
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == generic))
                {
                    return type;
                }
                if (generic.IsInterface)
                {
                    foreach (Type type2 in type.GetInterfaces())
                    {
                        Type type3 = FindGenericType(generic, type2);
                        if (type3 != null)
                        {
                            return type3;
                        }
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        private int FindIndexer(Type type, Expression[] args, out MethodBase method)
        {
            foreach (Type type2 in SelfAndBaseTypes(type))
            {
                MemberInfo[] defaultMembers = type2.GetDefaultMembers();
                if (defaultMembers.Length != 0)
                {
                    IEnumerable<MethodBase> methods = from p in defaultMembers.OfType<PropertyInfo>()
                                                      select p.GetGetMethod() into m
                                                      where m != null
                                                      select m;
                    int num = this.FindBestMethod(methods, args, out method);
                    if (num != 0)
                    {
                        return num;
                    }
                }
            }
            method = null;
            return 0;
        }

        private int FindMethod(Type type, string methodName, bool staticAccess, Expression[] args, out MethodBase method)
        {
            BindingFlags bindingAttr = (BindingFlags.Public | BindingFlags.DeclaredOnly) | (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type type2 in SelfAndBaseTypes(type))
            {
                MemberInfo[] source = type2.FindMembers(MemberTypes.Method, bindingAttr, Type.FilterNameIgnoreCase, methodName);
                int num = this.FindBestMethod(source.Cast<MethodBase>(), args, out method);
                if (num != 0)
                {
                    return num;
                }
            }
            method = null;
            return 0;
        }

        private static MemberInfo FindPropertyOrField(Type type, string memberName, bool staticAccess)
        {
            BindingFlags bindingAttr = (BindingFlags.Public | BindingFlags.DeclaredOnly) | (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type type2 in SelfAndBaseTypes(type))
            {
                MemberInfo[] infoArray = type2.FindMembers(MemberTypes.Property | MemberTypes.Field, bindingAttr, Type.FilterNameIgnoreCase, memberName);
                if (infoArray.Length != 0)
                {
                    return infoArray[0];
                }
            }
            return null;
        }

        private static Expression GenerateAdd(Expression left, Expression right)
        {
            if ((left.Type == typeof(string)) && (right.Type == typeof(string)))
            {
                return GenerateStaticMethodCall("Concat", left, right);
            }
            return Expression.Add(left, right);
        }

        private static Expression GenerateEqual(Expression left, Expression right)
        {
            return Expression.Equal(left, right);
        }

        private static Expression GenerateGreaterThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThan(GenerateStaticMethodCall("Compare", left, right), Expression.Constant(0));
            }
            return Expression.GreaterThan(left, right);
        }

        private static Expression GenerateGreaterThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThanOrEqual(GenerateStaticMethodCall("Compare", left, right), Expression.Constant(0));
            }
            return Expression.GreaterThanOrEqual(left, right);
        }

        private static Expression GenerateLessThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThan(GenerateStaticMethodCall("Compare", left, right), Expression.Constant(0));
            }
            return Expression.LessThan(left, right);
        }

        private static Expression GenerateLessThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThanOrEqual(GenerateStaticMethodCall("Compare", left, right), Expression.Constant(0));
            }
            return Expression.LessThanOrEqual(left, right);
        }

        private static Expression GenerateNotEqual(Expression left, Expression right)
        {
            return Expression.NotEqual(left, right);
        }

        private static Expression GenerateStaticMethodCall(string methodName, Expression left, Expression right)
        {
            return Expression.Call(null, GetStaticMethod(methodName, left, right), new Expression[] { left, right });
        }

        private static Expression GenerateStringConcat(Expression left, Expression right)
        {
            if (left.Type.IsValueType)
            {
                left = Expression.Convert(left, typeof(object));
            }
            if (right.Type.IsValueType)
            {
                right = Expression.Convert(right, typeof(object));
            }
            return Expression.Call(null, typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) }), new Expression[] { left, right });
        }

        private static Expression GenerateSubtract(Expression left, Expression right)
        {
            return Expression.Subtract(left, right);
        }

        private string GetIdentifier()
        {
            this.ValidateToken(TokenId.Identifier, SR.IdentifierExpected);
            string text = this.token.text;
            if ((text.Length > 1) && (text[0] == '@'))
            {
                text = text.Substring(1);
            }
            return text;
        }

        private static Type GetNonNullableType(Type type)
        {
            if (!IsNullableType(type))
            {
                return type;
            }
            return type.GetGenericArguments()[0];
        }

        private static int GetNumericTypeKind(Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Char:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return 1;

                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                        return 2;

                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return 3;
                }
            }
            return 0;
        }

        private static MethodInfo GetStaticMethod(string methodName, Expression left, Expression right)
        {
            return left.Type.GetMethod(methodName, new Type[] { left.Type, right.Type });
        }

        internal static string GetTypeName(Type type)
        {
            Type nonNullableType = GetNonNullableType(type);
            string name = nonNullableType.Name;
            if (type != nonNullableType)
            {
                name = name + '?';
            }
            return name;
        }

        private static Exception IncompatibleOperandsError(string opName, Expression left, Expression right, int pos)
        {
            return ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.IncompatibleOperands, new object[] { opName, GetTypeName(left.Type), GetTypeName(right.Type) }), new object[0]);
        }

        private bool IsApplicable(MethodData method, Expression[] args)
        {
            if (method.Parameters.Length != args.Length)
            {
                return false;
            }
            Expression[] expressionArray = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo info = method.Parameters[i];
                if (info.IsOut)
                {
                    return false;
                }
                Expression expression = this.PromoteExpression(args[i], info.ParameterType, false);
                if (expression == null)
                {
                    return false;
                }
                expressionArray[i] = expression;
            }
            method.Args = expressionArray;
            return true;
        }

        private static bool IsBetterThan(Expression[] args, MethodData m1, MethodData m2)
        {
            bool flag = false;
            for (int i = 0; i < args.Length; i++)
            {
                int num2 = CompareConversions(args[i].Type, m1.Parameters[i].ParameterType, m2.Parameters[i].ParameterType);
                if (num2 < 0)
                {
                    return false;
                }
                if (num2 > 0)
                {
                    flag = true;
                }
            }
            return flag;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Legacy code.")]
        private static bool IsCompatibleWith(Type source, Type target)
        {
            if (source == target)
            {
                return true;
            }
            if (!target.IsValueType)
            {
                return target.IsAssignableFrom(source);
            }
            Type nonNullableType = GetNonNullableType(source);
            Type type = GetNonNullableType(target);
            if ((nonNullableType == source) || (type != target))
            {
                TypeCode code = nonNullableType.IsEnum ? TypeCode.Object : Type.GetTypeCode(nonNullableType);
                TypeCode code2 = type.IsEnum ? TypeCode.Object : Type.GetTypeCode(type);
                switch (code)
                {
                    case TypeCode.SByte:
                        switch (code2)
                        {
                            case TypeCode.SByte:
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.Byte:
                        switch (code2)
                        {
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.Int16:
                        switch (code2)
                        {
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.UInt16:
                        switch (code2)
                        {
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.Int32:
                        switch (code2)
                        {
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.UInt32:
                        switch (code2)
                        {
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.Int64:
                        switch (code2)
                        {
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.UInt64:
                        switch (code2)
                        {
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;

                    case TypeCode.Single:
                        switch (code2)
                        {
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return true;
                        }
                        break;

                    default:
                        if (nonNullableType == type)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private static bool IsEnumType(Type type)
        {
            return GetNonNullableType(type).IsEnum;
        }

        private static bool IsIdentifierPart(char ch)
        {
            return (1 << (int)char.GetUnicodeCategory(ch) & 295807) != 0;
        }

        private static bool IsIdentifierStart(char ch)
        {
            return (1 << (int)char.GetUnicodeCategory(ch) & 543) != 0;
        }

        private static bool IsNullableType(Type type)
        {
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        private static bool IsSignedIntegralType(Type type)
        {
            return (GetNumericTypeKind(type) == 2);
        }

        private static bool IsUnsignedIntegralType(Type type)
        {
            return (GetNumericTypeKind(type) == 3);
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Legacy code.")]
        private MappedMemberInfo MapDateFunction(string functionName)
        {
            if (functionName == "day")
            {
                return new MappedMemberInfo(typeof(DateTime), "Day", false, false);
            }
            if (functionName == "month")
            {
                return new MappedMemberInfo(typeof(DateTime), "Month", false, false);
            }
            if (functionName == "year")
            {
                return new MappedMemberInfo(typeof(DateTime), "Year", false, false);
            }
            if (functionName == "hour")
            {
                return new MappedMemberInfo(typeof(DateTime), "Hour", false, false);
            }
            if (functionName == "minute")
            {
                return new MappedMemberInfo(typeof(DateTime), "Minute", false, false);
            }
            if (functionName == "second")
            {
                return new MappedMemberInfo(typeof(DateTime), "Second", false, false);
            }
            return null;
        }

        private MappedMemberInfo MapFunction(string functionName)
        {
            MappedMemberInfo info = this.MapStringFunction(functionName);
            if (info != null)
            {
                return info;
            }
            info = this.MapDateFunction(functionName);
            if (info != null)
            {
                return info;
            }
            info = this.MapMathFunction(functionName);
            if (info != null)
            {
                return info;
            }
            return null;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Legacy code.")]
        private MappedMemberInfo MapMathFunction(string functionName)
        {
            if (functionName == "round")
            {
                return new MappedMemberInfo(typeof(Math), "Round", true, true);
            }
            if (functionName == "floor")
            {
                return new MappedMemberInfo(typeof(Math), "Floor", true, true);
            }
            if (functionName == "ceiling")
            {
                return new MappedMemberInfo(typeof(Math), "Ceiling", true, true);
            }
            return null;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Legacy code.")]
        private MappedMemberInfo MapStringFunction(string functionName)
        {
            if (functionName == "startswith")
            {
                return new MappedMemberInfo(typeof(string), "StartsWith", false, true);
            }
            if (functionName == "endswith")
            {
                return new MappedMemberInfo(typeof(string), "EndsWith", false, true);
            }
            if (functionName == "length")
            {
                return new MappedMemberInfo(typeof(string), "Length", false, false);
            }
            if (functionName == "toupper")
            {
                return new MappedMemberInfo(typeof(string), "ToUpper", false, true);
            }
            if (functionName == "tolower")
            {
                return new MappedMemberInfo(typeof(string), "ToLower", false, true);
            }
            if (functionName == "substringof")
            {
                return new MappedMemberInfo(typeof(string), "Contains", false, true)
                {
                    MapParams = delegate(Expression[] args)
                    {
                        Expression expression = args[0];
                        args[0] = args[1];
                        args[1] = expression;
                    }
                };
            }
            if (functionName == "indexof")
            {
                return new MappedMemberInfo(typeof(string), "IndexOf", false, true);
            }
            if (functionName == "replace")
            {
                return new MappedMemberInfo(typeof(string), "Replace", false, true);
            }
            if (functionName == "substring")
            {
                return new MappedMemberInfo(typeof(string), "Substring", false, true);
            }
            if (functionName == "trim")
            {
                return new MappedMemberInfo(typeof(string), "Trim", false, true);
            }
            if (functionName == "concat")
            {
                return new MappedMemberInfo(typeof(string), "Concat", true, true);
            }
            return null;
        }

        private void NextChar()
        {
            if (this.textPos < this.textLen)
            {
                this.textPos++;
            }
            this.ch = (this.textPos < this.textLen) ? this.text[this.textPos] : '\0';
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Legacy code.")]
        private void NextToken()
        {
            TokenId openBracket;
            while (char.IsWhiteSpace(this.ch))
            {
                this.NextChar();
            }
            int textPos = this.textPos;
            switch (this.ch)
            {
                case '[':
                    this.NextChar();
                    openBracket = TokenId.OpenBracket;
                    break;

                case ']':
                    this.NextChar();
                    openBracket = TokenId.CloseBracket;
                    break;

                case '|':
                    this.NextChar();
                    if (this.ch == '|')
                    {
                        this.NextChar();
                        openBracket = TokenId.DoubleBar;
                    }
                    else
                    {
                        openBracket = TokenId.Bar;
                    }
                    break;

                case '?':
                    this.NextChar();
                    openBracket = TokenId.Question;
                    break;

                case '"':
                case '\'':
                    {
                        char ch = this.ch;
                        do
                        {
                            this.NextChar();
                            while ((this.textPos < this.textLen) && (this.ch != ch))
                            {
                                if (this.ch == '\\')
                                {
                                    this.NextChar();
                                }
                                this.NextChar();
                            }
                            if (this.textPos == this.textLen)
                            {
                                throw ParseError(this.textPos, SR.UnterminatedStringLiteral, new object[0]);
                            }
                            this.NextChar();
                        }
                        while (this.ch == ch);
                        openBracket = TokenId.StringLiteral;
                        break;
                    }
                case '%':
                    this.NextChar();
                    openBracket = TokenId.Percent;
                    break;

                case '&':
                    this.NextChar();
                    if (this.ch != '&')
                    {
                        openBracket = TokenId.Amphersand;
                    }
                    else
                    {
                        this.NextChar();
                        openBracket = TokenId.DoubleAmphersand;
                    }
                    break;

                case '(':
                    this.NextChar();
                    openBracket = TokenId.OpenParen;
                    break;

                case ')':
                    this.NextChar();
                    openBracket = TokenId.CloseParen;
                    break;

                case ',':
                    this.NextChar();
                    openBracket = TokenId.Comma;
                    break;

                case '-':
                    this.NextChar();
                    openBracket = TokenId.Minus;
                    break;

                case '/':
                    this.NextChar();
                    openBracket = TokenId.Dot;
                    break;

                case ':':
                    this.NextChar();
                    openBracket = TokenId.Colon;
                    break;

                default:
                    if ((IsIdentifierStart(this.ch) || (this.ch == '@')) || (this.ch == '_'))
                    {
                        do
                        {
                            this.NextChar();
                        }
                        while (IsIdentifierPart(this.ch) || (this.ch == '_'));
                        openBracket = TokenId.Identifier;
                    }
                    else if (char.IsDigit(this.ch))
                    {
                        openBracket = TokenId.IntegerLiteral;
                        do
                        {
                            this.NextChar();
                        }
                        while (char.IsDigit(this.ch));
                        if (this.ch == '.')
                        {
                            openBracket = TokenId.RealLiteral;
                            this.NextChar();
                            this.ValidateDigit();
                            do
                            {
                                this.NextChar();
                            }
                            while (char.IsDigit(this.ch));
                        }
                        if ((this.ch == 'E') || (this.ch == 'e'))
                        {
                            openBracket = TokenId.RealLiteral;
                            this.NextChar();
                            if ((this.ch == '+') || (this.ch == '-'))
                            {
                                this.NextChar();
                            }
                            this.ValidateDigit();
                            do
                            {
                                this.NextChar();
                            }
                            while (char.IsDigit(this.ch));
                        }
                        if ((((this.ch == 'F') || (this.ch == 'f')) || ((this.ch == 'M') || (this.ch == 'm'))) || ((this.ch == 'D') || (this.ch == 'd')))
                        {
                            openBracket = TokenId.RealLiteral;
                            this.NextChar();
                        }
                    }
                    else
                    {
                        if (this.textPos != this.textLen)
                        {
                            throw ParseError(this.textPos, string.Format(CultureInfo.CurrentCulture, SR.InvalidCharacter, new object[] { this.ch }), new object[0]);
                        }
                        openBracket = TokenId.End;
                    }
                    break;
            }
            this.token.id = openBracket;
            this.token.text = this.text.Substring(textPos, this.textPos - textPos);
            this.token.pos = textPos;
            this.token.id = this.ReclassifyToken(this.token);
        }

        public Expression Parse(Type resultType)
        {
            int pos = this.token.pos;
            Expression expr = this.ParseExpression();
            if ((resultType != null) && ((expr = this.PromoteExpression(expr, resultType, true)) == null))
            {
                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.ExpressionTypeMismatch, new object[] { GetTypeName(resultType) }), new object[0]);
            }
            this.ValidateToken(TokenId.End, SR.SyntaxError);
            return expr;
        }

        private Expression ParseAdditive()
        {
            Expression left = this.ParseMultiplicative();
            while (((this.token.id == TokenId.Plus) || (this.token.id == TokenId.Minus)) || (this.token.id == TokenId.Amphersand))
            {
                Token token = this.token;
                this.NextToken();
                Expression right = this.ParseMultiplicative();
                switch (token.id)
                {
                    case TokenId.Plus:
                        {
                            if ((left.Type == typeof(string)) || (right.Type == typeof(string)))
                            {
                                break;
                            }
                            this.CheckAndPromoteOperands(typeof(IAddSignatures), token.text, ref left, ref right, token.pos);
                            left = GenerateAdd(left, right);
                            continue;
                        }
                    case TokenId.Comma:
                        {
                            continue;
                        }
                    case TokenId.Minus:
                        {
                            this.CheckAndPromoteOperands(typeof(ISubtractSignatures), token.text, ref left, ref right, token.pos);
                            left = GenerateSubtract(left, right);
                            continue;
                        }
                    case TokenId.Amphersand:
                        break;

                    default:
                        {
                            continue;
                        }
                }
                left = GenerateStringConcat(left, right);
            }
            return left;
        }

        private Expression ParseAggregate(Expression instance, Type elementType, string methodName, int errorPos)
        {
            MethodBase base2;
            Type[] typeArray;
            ParameterExpression it = this.it;
            ParameterExpression expression2 = Expression.Parameter(elementType, "");
            this.it = expression2;
            Expression[] args = this.ParseArgumentList();
            this.it = it;
            if (this.FindMethod(typeof(IEnumerableSignatures), methodName, false, args, out base2) != 1)
            {
                throw ParseError(errorPos, string.Format(CultureInfo.CurrentCulture, SR.NoApplicableAggregate, new object[] { methodName }), new object[0]);
            }
            if ((base2.Name == "Min") || (base2.Name == "Max"))
            {
                typeArray = new Type[] { elementType, args[0].Type };
            }
            else
            {
                typeArray = new Type[] { elementType };
            }
            if (args.Length == 0)
            {
                args = new Expression[] { instance };
            }
            else
            {
                args = new Expression[] { instance, Query.DynamicExpression.Lambda(args[0], new ParameterExpression[] { expression2 }) };
            }
            return Expression.Call(typeof(Enumerable), base2.Name, typeArray, args);
        }

        private Expression[] ParseArgumentList()
        {
            this.ValidateToken(TokenId.OpenParen, SR.OpenParenExpected);
            this.NextToken();
            Expression[] expressionArray = (this.token.id != TokenId.CloseParen) ? this.ParseArguments() : new Expression[0];
            this.ValidateToken(TokenId.CloseParen, SR.CloseParenOrCommaExpected);
            this.NextToken();
            return expressionArray;
        }

        private Expression[] ParseArguments()
        {
            List<Expression> list = new List<Expression>();
            while (true)
            {
                list.Add(this.ParseExpression());
                if (this.token.id != TokenId.Comma)
                {
                    break;
                }
                this.NextToken();
            }
            return list.ToArray();
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Legacy code.")]
        private Expression ParseComparison()
        {
            Expression left = this.ParseAdditive();
            while ((((this.token.id == TokenId.Equal) || (this.token.id == TokenId.DoubleEqual)) || ((this.token.id == TokenId.ExclamationEqual) || (this.token.id == TokenId.LessGreater))) || (((this.token.id == TokenId.GreaterThan) || (this.token.id == TokenId.GreaterThanEqual)) || ((this.token.id == TokenId.LessThan) || (this.token.id == TokenId.LessThanEqual))))
            {
                Token token = this.token;
                this.NextToken();
                Expression right = this.ParseAdditive();
                bool flag = (((token.id == TokenId.Equal) || (token.id == TokenId.DoubleEqual)) || (token.id == TokenId.ExclamationEqual)) || (token.id == TokenId.LessGreater);
                if ((flag && !left.Type.IsValueType) && !right.Type.IsValueType)
                {
                    if (left.Type != right.Type)
                    {
                        if (!left.Type.IsAssignableFrom(right.Type))
                        {
                            if (!right.Type.IsAssignableFrom(left.Type))
                            {
                                throw IncompatibleOperandsError(token.text, left, right, token.pos);
                            }
                            left = Expression.Convert(left, right.Type);
                        }
                        else
                        {
                            right = Expression.Convert(right, left.Type);
                        }
                    }
                }
                else if (IsEnumType(left.Type) || IsEnumType(right.Type))
                {
                    left = ConvertEnumExpression(left, right);
                    right = ConvertEnumExpression(right, left);
                    this.CheckAndPromoteOperands(flag ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures), token.text, ref left, ref right, token.pos);
                }
                else
                {
                    this.CheckAndPromoteOperands(flag ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures), token.text, ref left, ref right, token.pos);
                }
                switch (token.id)
                {
                    case TokenId.LessThan:
                        left = GenerateLessThan(left, right);
                        break;

                    case TokenId.Equal:
                    case TokenId.DoubleEqual:
                        left = GenerateEqual(left, right);
                        break;

                    case TokenId.GreaterThan:
                        left = GenerateGreaterThan(left, right);
                        break;

                    case TokenId.ExclamationEqual:
                    case TokenId.LessGreater:
                        left = GenerateNotEqual(left, right);
                        break;

                    case TokenId.LessThanEqual:
                        left = GenerateLessThanEqual(left, right);
                        break;

                    case TokenId.GreaterThanEqual:
                        left = GenerateGreaterThanEqual(left, right);
                        break;
                }
            }
            return left;
        }

        private Expression ParseElementAccess(Expression expr)
        {
            MethodBase base2;
            int pos = this.token.pos;
            this.ValidateToken(TokenId.OpenBracket, SR.OpenParenExpected);
            this.NextToken();
            Expression[] args = this.ParseArguments();
            this.ValidateToken(TokenId.CloseBracket, SR.CloseBracketOrCommaExpected);
            this.NextToken();
            if (expr.Type.IsArray)
            {
                if ((expr.Type.GetArrayRank() != 1) || (args.Length != 1))
                {
                    throw ParseError(pos, SR.CannotIndexMultiDimArray, new object[0]);
                }
                Expression index = this.PromoteExpression(args[0], typeof(int), true);
                if (index == null)
                {
                    throw ParseError(pos, SR.InvalidIndex, new object[0]);
                }
                return Expression.ArrayIndex(expr, index);
            }
            switch (this.FindIndexer(expr.Type, args, out base2))
            {
                case 0:
                    throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.NoApplicableIndexer, new object[] { GetTypeName(expr.Type) }), new object[0]);

                case 1:
                    return Expression.Call(expr, (MethodInfo)base2, args);
            }
            throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.AmbiguousIndexerInvocation, new object[] { GetTypeName(expr.Type) }), new object[0]);
        }

        private static object ParseEnum(string name, Type type)
        {
            if (type.IsEnum)
            {
                MemberInfo[] infoArray = type.FindMembers(MemberTypes.Field, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, Type.FilterNameIgnoreCase, name);
                if (infoArray.Length != 0)
                {
                    return ((FieldInfo)infoArray[0]).GetValue(null);
                }
            }
            return null;
        }

        private Exception ParseError(string format, params object[] args)
        {
            return ParseError(this.token.pos, format, args);
        }

        private static Exception ParseError(int pos, string format, params object[] args)
        {
            return new ParseException(string.Format(CultureInfo.CurrentCulture, format, args), pos);
        }

        private Expression ParseExpression()
        {
            return this.ParseLogicalOr();
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Legacy code.")]
        private Expression ParseIdentifier()
        {
            object obj2;
            this.ValidateToken(TokenId.Identifier);
            if (keywords.TryGetValue(this.token.text, out obj2))
            {
                if (obj2 is Type)
                {
                    return this.ParseTypeConstruction((Type)obj2);
                }
                this.NextToken();
                return (Expression)obj2;
            }
            if (this.symbols.TryGetValue(this.token.text, out obj2))
            {
                Expression expression = obj2 as Expression;
                if (expression == null)
                {
                    expression = Expression.Constant(obj2);
                }
                this.NextToken();
                return expression;
            }
            MappedMemberInfo mappedMember = this.MapFunction(this.token.text);
            if (mappedMember != null)
            {
                return this.ParseMappedFunction(mappedMember);
            }
            if (this.it == null)
            {
                throw this.ParseError(string.Format(CultureInfo.CurrentCulture, SR.UnknownIdentifier, new object[] { this.token.text }), new object[0]);
            }
            return this.ParseMemberAccess(null, this.it);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", Justification = "Legacy code.")]
        private Expression ParseIntegerLiteral()
        {
            long num2;
            this.ValidateToken(TokenId.IntegerLiteral);
            string text = this.token.text;
            if (text[0] != '-')
            {
                ulong num;
                if (!ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out num))
                {
                    throw this.ParseError(string.Format(CultureInfo.CurrentCulture, SR.InvalidIntegerLiteral, new object[] { text }), new object[0]);
                }
                this.NextToken();
                if ((this.token.text == "L") || (this.token.text == "l"))
                {
                    this.NextToken();
                    return this.CreateLiteral((long)num, text);
                }
                if (num <= 0x7fffffffL)
                {
                    return this.CreateLiteral((int)num, text);
                }
                if (num <= 0xffffffffL)
                {
                    return this.CreateLiteral((uint)num, text);
                }
                if (num <= 0x7fffffffffffffffL)
                {
                    return this.CreateLiteral((long)num, text);
                }
                return this.CreateLiteral(num, text);
            }
            if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num2))
            {
                throw this.ParseError(string.Format(CultureInfo.CurrentCulture, SR.InvalidIntegerLiteral, new object[] { text }), new object[0]);
            }
            this.NextToken();
            if ((this.token.text == "L") || (this.token.text == "l"))
            {
                this.NextToken();
                return this.CreateLiteral(num2, text);
            }
            if ((num2 >= -2147483648L) && (num2 <= 0x7fffffffL))
            {
                return this.CreateLiteral((int)num2, text);
            }
            return this.CreateLiteral(num2, text);
        }

        private Expression ParseLogicalAnd()
        {
            Expression left = this.ParseComparison();
            while ((this.token.id == TokenId.DoubleAmphersand) || this.TokenIdentifierIs("and"))
            {
                Token token = this.token;
                this.NextToken();
                Expression right = this.ParseComparison();
                this.CheckAndPromoteOperands(typeof(ILogicalSignatures), token.text, ref left, ref right, token.pos);
                left = Expression.AndAlso(left, right);
            }
            return left;
        }

        private Expression ParseLogicalOr()
        {
            Expression left = this.ParseLogicalAnd();
            while ((this.token.id == TokenId.DoubleBar) || this.TokenIdentifierIs("or"))
            {
                Token token = this.token;
                this.NextToken();
                Expression right = this.ParseLogicalAnd();
                this.CheckAndPromoteOperands(typeof(ILogicalSignatures), token.text, ref left, ref right, token.pos);
                left = Expression.OrElse(left, right);
            }
            return left;
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Legacy code.")]
        private Expression ParseMappedFunction(MappedMemberInfo mappedMember)
        {
            Type mappedType = mappedMember.MappedType;
            string memberName = mappedMember.MemberName;
            int pos = this.token.pos;
            Expression[] expressionArray = null;
            Expression expression = null;
            Expression instance = null;
            this.NextToken();
            if (this.token.id == TokenId.OpenParen)
            {
                expressionArray = this.ParseArgumentList();
                if (mappedMember.MapParams != null)
                {
                    mappedMember.MapParams(expressionArray);
                }
                expression = expressionArray[0];
                instance = expression;
                if (!mappedMember.IsStatic)
                {
                    expressionArray = expressionArray.Skip<Expression>(1).ToArray<Expression>();
                }
                else
                {
                    instance = null;
                }
            }
            if (mappedMember.IsMethod)
            {
                MethodBase base2;
                switch (this.FindMethod(mappedType, memberName, mappedMember.IsStatic, expressionArray, out base2))
                {
                    case 0:
                        throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.NoApplicableMethod, new object[] { memberName, GetTypeName(mappedType) }), new object[0]);

                    case 1:
                        {
                            MethodInfo method = (MethodInfo)base2;
                            if (method.ReturnType == typeof(void))
                            {
                                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.MethodIsVoid, new object[] { memberName, GetTypeName(method.DeclaringType) }), new object[0]);
                            }
                            return Expression.Call(instance, method, expressionArray);
                        }
                }
                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.AmbiguousMethodInvocation, new object[] { memberName, GetTypeName(mappedType) }), new object[0]);
            }
            MemberInfo info2 = FindPropertyOrField(mappedType, memberName, mappedMember.IsStatic);
            if (info2 == null)
            {
                if (this.queryResolver != null)
                {
                    MemberExpression expression3 = this.queryResolver.ResolveMember(mappedType, memberName, instance);
                    if (expression3 != null)
                    {
                        return expression3;
                    }
                }
                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.UnknownPropertyOrField, new object[] { memberName, GetTypeName(mappedType) }), new object[0]);
            }
            if (info2 is PropertyInfo)
            {
                return Expression.Property(instance, (PropertyInfo)info2);
            }
            return Expression.Field(instance, (FieldInfo)info2);
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Legacy code.")]
        private Expression ParseMemberAccess(Type type, Expression instance)
        {
            if (instance != null)
            {
                type = instance.Type;
            }
            int pos = this.token.pos;
            string identifier = this.GetIdentifier();
            this.NextToken();
            if (this.token.id == TokenId.OpenParen)
            {
                if ((instance != null) && (type != typeof(string)))
                {
                    Type type2 = FindGenericType(typeof(IEnumerable<>), type);
                    if (type2 != null)
                    {
                        Type elementType = type2.GetGenericArguments()[0];
                        return this.ParseAggregate(instance, elementType, identifier, pos);
                    }
                }
                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.UnknownIdentifier, new object[] { identifier }), new object[0]);
            }
            MemberInfo info = FindPropertyOrField(type, identifier, instance == null);
            if (info == null)
            {
                if (this.queryResolver != null)
                {
                    MemberExpression expression = this.queryResolver.ResolveMember(type, identifier, instance);
                    if (expression != null)
                    {
                        return expression;
                    }
                }
                throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.UnknownPropertyOrField, new object[] { identifier, GetTypeName(type) }), new object[0]);
            }
            if (info is PropertyInfo)
            {
                return Expression.Property(instance, (PropertyInfo)info);
            }
            return Expression.Field(instance, (FieldInfo)info);
        }

        private Expression ParseMultiplicative()
        {
            Expression expression = this.ParseUnary();
            while (this.token.id == TokenId.Asterisk || this.token.id == TokenId.Slash || this.token.id == TokenId.Percent || this.TokenIdentifierIs("mod"))
            {
                Token token = this.token;
                this.NextToken();
                Expression right = this.ParseUnary();
                this.CheckAndPromoteOperands(typeof(IArithmeticSignatures), token.text, ref expression, ref right, token.pos);
                TokenId id = token.id;
                if (id <= TokenId.Percent)
                {
                    if (id == TokenId.Identifier || id == TokenId.Percent)
                    {
                        expression = Expression.Modulo(expression, right);
                    }
                }
                else
                {
                    if (id != ExpressionParser.TokenId.Asterisk)
                    {
                        if (id == ExpressionParser.TokenId.Slash)
                        {
                            expression = Expression.Divide(expression, right);
                        }
                    }
                    else
                    {
                        expression = Expression.Multiply(expression, right);
                    }
                }
            }
            return expression;
        }

        private static object ParseNumber(string text, Type type)
        {
            switch (Type.GetTypeCode(GetNonNullableType(type)))
            {
                case TypeCode.SByte:
                    sbyte num;
                    if (!sbyte.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num))
                    {
                        break;
                    }
                    return num;

                case TypeCode.Byte:
                    byte num2;
                    if (!byte.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out num2))
                    {
                        break;
                    }
                    return num2;

                case TypeCode.Int16:
                    short num3;
                    if (!short.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num3))
                    {
                        break;
                    }
                    return num3;

                case TypeCode.UInt16:
                    ushort num4;
                    if (!ushort.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out num4))
                    {
                        break;
                    }
                    return num4;

                case TypeCode.Int32:
                    int num5;
                    if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num5))
                    {
                        break;
                    }
                    return num5;

                case TypeCode.UInt32:
                    uint num6;
                    if (!uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out num6))
                    {
                        break;
                    }
                    return num6;

                case TypeCode.Int64:
                    long num7;
                    if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num7))
                    {
                        break;
                    }
                    return num7;

                case TypeCode.UInt64:
                    ulong num8;
                    if (!ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out num8))
                    {
                        break;
                    }
                    return num8;

                case TypeCode.Single:
                    float num9;
                    if (!float.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out num9))
                    {
                        break;
                    }
                    return num9;

                case TypeCode.Double:
                    double num10;
                    if (!double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out num10))
                    {
                        break;
                    }
                    return num10;

                case TypeCode.Decimal:
                    decimal num11;
                    if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out num11))
                    {
                        break;
                    }
                    return num11;
            }
            return null;
        }

        public IEnumerable<DynamicOrdering> ParseOrdering()
        {
            List<DynamicOrdering> list = new List<DynamicOrdering>();
            while (true)
            {
                Expression selector = this.ParseExpression();
                bool ascending = true;
                if (this.TokenIdentifierIs("asc") || this.TokenIdentifierIs("ascending"))
                {
                    this.NextToken();
                }
                else
                {
                    if (this.TokenIdentifierIs("desc") || this.TokenIdentifierIs("descending"))
                    {
                        this.NextToken();
                        ascending = false;
                    }
                }
                list.Add(new DynamicOrdering
                {
                    Selector = selector,
                    Ascending = ascending
                });
                if (this.token.id != ExpressionParser.TokenId.Comma)
                {
                    break;
                }
                this.NextToken();
            }
            this.ValidateToken(ExpressionParser.TokenId.End, SR.SyntaxError);
            return list;
        }

        private Expression ParseParenExpression()
        {
            this.ValidateToken(TokenId.OpenParen, SR.OpenParenExpected);
            this.NextToken();
            Expression expression = this.ParseExpression();
            this.ValidateToken(TokenId.CloseParen, SR.CloseParenOrOperatorExpected);
            this.NextToken();
            return expression;
        }

        private Expression ParsePrimary()
        {
            Expression expression = this.ParsePrimaryStart();
            while (true)
            {
                if (this.token.id == TokenId.Dot)
                {
                    this.NextToken();
                    expression = this.ParseMemberAccess(null, expression);
                }
                else
                {
                    if (this.token.id != TokenId.OpenBracket)
                    {
                        break;
                    }
                    expression = this.ParseElementAccess(expression);
                }
            }
            return expression;
        }

        private Expression ParsePrimaryStart()
        {
            switch (this.token.id)
            {
                case TokenId.Identifier:
                    return this.ParseIdentifier();

                case TokenId.StringLiteral:
                    return this.ParseStringLiteral();

                case TokenId.IntegerLiteral:
                    return this.ParseIntegerLiteral();

                case TokenId.RealLiteral:
                    return this.ParseRealLiteral();

                case TokenId.OpenParen:
                    return this.ParseParenExpression();
            }
            throw this.ParseError(SR.ExpressionExpected, new object[0]);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", Justification = "Legacy code.")]
        private Expression ParseRealLiteral()
        {
            this.ValidateToken(TokenId.RealLiteral);
            string text = this.token.text;
            object obj2 = null;
            switch (text[text.Length - 1])
            {
                case 'F':
                case 'f':
                    float num;
                    if (float.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowTrailingSign, CultureInfo.InvariantCulture, out num))
                    {
                        obj2 = num;
                    }
                    break;

                case 'M':
                case 'm':
                    decimal num2;
                    if (decimal.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowTrailingSign, CultureInfo.InvariantCulture, out num2))
                    {
                        obj2 = num2;
                    }
                    break;

                case 'D':
                case 'd':
                    double num3;
                    if (double.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowTrailingSign, CultureInfo.InvariantCulture, out num3))
                    {
                        obj2 = num3;
                    }
                    break;

                default:
                    double num4;
                    if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowTrailingSign, CultureInfo.InvariantCulture, out num4))
                    {
                        obj2 = num4;
                    }
                    break;
            }
            if (obj2 == null)
            {
                throw this.ParseError(string.Format(CultureInfo.CurrentCulture, SR.InvalidRealLiteral, new object[] { text }), new object[0]);
            }
            this.NextToken();
            return this.CreateLiteral(obj2, text);
        }

        private Expression ParseStringLiteral()
        {
            this.ValidateToken(TokenId.StringLiteral);
            char ch = this.token.text[0];
            string str = this.token.text.Substring(1, this.token.text.Length - 2).Replace(@"\\", @"\");
            if (ch == '\'')
            {
                str = str.Replace(@"\'", "'");
            }
            else
            {
                str = str.Replace("\\\"", "\"");
            }
            this.NextToken();
            return this.CreateLiteral(str, str);
        }

        private Expression ParseTypeConstruction(Type type)
        {
            string text = this.token.text;
            int pos = this.token.pos;
            this.NextToken();
            Expression expression = null;
            if (this.token.id == TokenId.StringLiteral)
            {
                pos = this.token.pos;
                string s = (string)((ConstantExpression)this.ParseStringLiteral()).Value;
                try
                {
                    if (type == typeof(DateTime))
                    {
                        return Expression.Constant(DateTime.Parse(s, CultureInfo.CurrentCulture));
                    }
                    if (type == typeof(Guid))
                    {
                        expression = Expression.Constant(Guid.Parse(s));
                    }
                    if (type == typeof(DateTimeOffset))
                    {
                        expression = Expression.Constant(DateTimeOffset.Parse(s, CultureInfo.CurrentCulture));
                    }
                    if (type == typeof(byte[]))
                    {
                        if ((s.Length % 2) != 0)
                        {
                            throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.InvalidHexLiteral, new object[0]), new object[0]);
                        }
                        byte[] buffer = new byte[s.Length / 2];
                        int startIndex = 0;
                        for (int i = 0; startIndex < s.Length; i++)
                        {
                            string str3 = s.Substring(startIndex, 2);
                            buffer[i] = byte.Parse(str3, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            startIndex += 2;
                        }
                        expression = Expression.Constant(buffer);
                    }
                    if (type == typeof(TimeSpan))
                    {
                        expression = Expression.Constant(TimeSpan.Parse(s, CultureInfo.CurrentCulture));
                    }
                }
                catch (FormatException exception)
                {
                    throw ParseError(pos, exception.Message, new object[0]);
                }
            }
            throw ParseError(pos, string.Format(CultureInfo.CurrentCulture, SR.InvalidTypeCreationExpression, new object[] { text }), new object[0]);
        }

        private Expression ParseUnary()
        {
            if (((this.token.id != TokenId.Minus) && (this.token.id != TokenId.Exclamation)) && !this.TokenIdentifierIs("not"))
            {
                return this.ParsePrimary();
            }
            Token token = this.token;
            this.NextToken();
            if ((token.id == TokenId.Minus) && ((this.token.id == TokenId.IntegerLiteral) || (this.token.id == TokenId.RealLiteral)))
            {
                this.token.text = "-" + this.token.text;
                this.token.pos = token.pos;
                return this.ParsePrimary();
            }
            Expression expr = this.ParseUnary();
            if (token.id == TokenId.Minus)
            {
                this.CheckAndPromoteOperand(typeof(INegationSignatures), token.text, ref expr, token.pos);
                return Expression.Negate(expr);
            }
            this.CheckAndPromoteOperand(typeof(INotSignatures), token.text, ref expr, token.pos);
            return Expression.Not(expr);
        }

        private void ProcessParameters(ParameterExpression[] parameters)
        {
            foreach (ParameterExpression expression in parameters)
            {
                if (!string.IsNullOrEmpty(expression.Name))
                {
                    this.AddSymbol(expression.Name, expression);
                }
            }
            if ((parameters.Length == 1) && string.IsNullOrEmpty(parameters[0].Name))
            {
                this.it = parameters[0];
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", Justification = "Legacy code."), SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Legacy code.")]
        private Expression PromoteExpression(Expression expr, Type type, bool exact)
        {
            if (expr.Type == type)
            {
                return expr;
            }
            if (expr is ConstantExpression)
            {
                ConstantExpression key = (ConstantExpression)expr;
                if (key == nullLiteral)
                {
                    if (!type.IsValueType || IsNullableType(type))
                    {
                        return Expression.Constant(null, type);
                    }
                }
                else
                {
                    string str;
                    if (this.literals.TryGetValue(key, out str))
                    {
                        Type nonNullableType = GetNonNullableType(type);
                        object obj2 = null;
                        switch (Type.GetTypeCode(key.Type))
                        {
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                if (!nonNullableType.IsEnum)
                                {
                                    if (nonNullableType == typeof(char))
                                    {
                                        obj2 = Convert.ToChar(key.Value, CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        obj2 = ParseNumber(str, nonNullableType);
                                    }
                                    break;
                                }
                                obj2 = Enum.Parse(nonNullableType, str);
                                break;

                            case TypeCode.Double:
                                if (nonNullableType == typeof(decimal))
                                {
                                    obj2 = ParseNumber(str, nonNullableType);
                                }
                                break;

                            case TypeCode.String:
                                obj2 = ParseEnum(str, nonNullableType);
                                break;
                        }
                        if (obj2 != null)
                        {
                            return Expression.Constant(obj2, type);
                        }
                    }
                }
            }
            if (!IsCompatibleWith(expr.Type, type))
            {
                return null;
            }
            if (!type.IsValueType && !exact)
            {
                return expr;
            }
            return Expression.Convert(expr, type);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", Justification = "Legacy code."), SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Legacy code.")]
        private TokenId ReclassifyToken(Token token)
        {
            if (token.id == TokenId.Identifier)
            {
                if (token.text == "add")
                {
                    return TokenId.Plus;
                }
                if (token.text == "and")
                {
                    return TokenId.DoubleAmphersand;
                }
                if (token.text == "div")
                {
                    return TokenId.Slash;
                }
                if (token.text == "sub")
                {
                    return TokenId.Minus;
                }
                if (token.text == "mul")
                {
                    return TokenId.Asterisk;
                }
                if (token.text == "mod")
                {
                    return TokenId.Percent;
                }
                if (token.text == "ne")
                {
                    return TokenId.ExclamationEqual;
                }
                if (token.text == "not")
                {
                    return TokenId.Exclamation;
                }
                if (token.text == "le")
                {
                    return TokenId.LessThanEqual;
                }
                if (token.text == "lt")
                {
                    return TokenId.LessThan;
                }
                if (token.text == "eq")
                {
                    return TokenId.DoubleEqual;
                }
                if (token.text == "eq")
                {
                    return TokenId.DoubleEqual;
                }
                if (token.text == "ge")
                {
                    return TokenId.GreaterThanEqual;
                }
                if (token.text == "gt")
                {
                    return TokenId.GreaterThan;
                }
            }
            return token.id;
        }

        private static IEnumerable<Type> SelfAndBaseClasses(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
            yield break;
        }

        private static IEnumerable<Type> SelfAndBaseTypes(Type type)
        {
            if (type.IsInterface)
            {
                List<Type> types = new List<Type>();
                AddInterface(types, type);
                return types;
            }
            return SelfAndBaseClasses(type);
        }

        private void SetTextPos(int pos)
        {
            this.textPos = pos;
            this.ch = (this.textPos < this.textLen) ? this.text[this.textPos] : '\0';
        }

        private bool TokenIdentifierIs(string id)
        {
            return ((this.token.id == TokenId.Identifier) && string.Equals(id, this.token.text, StringComparison.OrdinalIgnoreCase));
        }

        private void ValidateDigit()
        {
            if (!char.IsDigit(this.ch))
            {
                throw ParseError(this.textPos, SR.DigitExpected, new object[0]);
            }
        }

        private void ValidateToken(TokenId t)
        {
            if (this.token.id != t)
            {
                throw this.ParseError(SR.SyntaxError, new object[0]);
            }
        }

        private void ValidateToken(TokenId t, string errorMessage)
        {
            if (this.token.id != t)
            {
                throw this.ParseError(errorMessage, new object[0]);
            }
        }

        private interface IAddSignatures : ExpressionParser.IArithmeticSignatures
        {
            void F(DateTime x, TimeSpan y);
            void F(DateTime? x, TimeSpan? y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(DateTimeOffset? x, TimeSpan? y);
            void F(TimeSpan? x, TimeSpan? y);
            void F(DateTimeOffset x, TimeSpan y);
            void F(TimeSpan x, TimeSpan y);
        }

        private interface IArithmeticSignatures
        {
            void F(decimal x, decimal y);
            void F(double x, double y);
            void F(int x, int y);
            void F(decimal? x, decimal? y);
            void F(double? x, double? y);
            void F(long x, long y);
            void F(int? x, int? y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(long? x, long? y);
            void F(float? x, float? y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(uint? x, uint? y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(ulong? x, ulong? y);
            void F(float x, float y);
            void F(uint x, uint y);
            void F(ulong x, ulong y);
        }

        private interface IEnumerableSignatures
        {
            void All(bool predicate);
            void Any();
            void Any(bool predicate);
            void Average(decimal? selector);
            void Average(decimal selector);
            void Average(double selector);
            void Average(long? selector);
            void Average(int selector);
            void Average(float? selector);
            void Average(long selector);
            void Average(double? selector);
            void Average(int? selector);
            void Average(float selector);
            void Count();
            void Count(bool predicate);
            void Max(object selector);
            void Min(object selector);
            void Sum(decimal? selector);
            void Sum(decimal selector);
            void Sum(double? selector);
            void Sum(int? selector);
            void Sum(long? selector);
            void Sum(double selector);
            void Sum(int selector);
            void Sum(long selector);
            void Sum(float? selector);
            void Sum(float selector);
            void Where(bool predicate);
        }

        private interface IEqualitySignatures : ExpressionParser.IRelationalSignatures, ExpressionParser.IArithmeticSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
            void F(Guid x, Guid y);
            void F(Guid? x, Guid? y);
        }

        private interface ILogicalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        private interface INegationSignatures
        {
            void F(decimal x);
            void F(double x);
            void F(int x);
            void F(decimal? x);
            void F(long x);
            void F(double? x);
            void F(int? x);
            void F(long? x);
            void F(float? x);
            void F(float x);
        }

        private interface INotSignatures
        {
            void F(bool x);
            void F(bool? x);
        }

        private interface IRelationalSignatures : ExpressionParser.IArithmeticSignatures
        {
            void F(char? x, char? y);
            void F(char x, char y);
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(DateTimeOffset? x, DateTimeOffset? y);
            void F(TimeSpan? x, TimeSpan? y);
            void F(DateTimeOffset x, DateTimeOffset y);
            void F(string x, string y);
            void F(TimeSpan x, TimeSpan y);
        }

        private interface ISubtractSignatures : ExpressionParser.IAddSignatures, ExpressionParser.IArithmeticSignatures
        {
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
            void F(DateTimeOffset x, DateTimeOffset y);
            [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "Legacy code.")]
            void F(DateTimeOffset? x, DateTimeOffset? y);
        }

        internal class MappedMemberInfo
        {
            public MappedMemberInfo(Type mappedType, string memberName, bool isStatic, bool isMethod)
            {
                this.MappedType = mappedType;
                this.MemberName = memberName;
                this.IsStatic = isStatic;
                this.IsMethod = isMethod;
            }

            public bool IsMethod { get; private set; }

            public bool IsStatic { get; private set; }

            public Action<Expression[]> MapParams { get; set; }

            public Type MappedType { get; private set; }

            public string MemberName { get; private set; }
        }

        private class MethodData
        {
            public Expression[] Args;
            public System.Reflection.MethodBase MethodBase;
            public ParameterInfo[] Parameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Token
        {
            public ExpressionParser.TokenId id;
            public string text;
            public int pos;
        }

        private enum TokenId
        {
            Unknown,
            End,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            RealLiteral,
            Exclamation,
            Percent,
            Amphersand,
            OpenParen,
            CloseParen,
            Asterisk,
            Plus,
            Comma,
            Minus,
            Dot,
            Slash,
            Colon,
            LessThan,
            Equal,
            GreaterThan,
            Question,
            OpenBracket,
            CloseBracket,
            Bar,
            ExclamationEqual,
            DoubleAmphersand,
            LessThanEqual,
            LessGreater,
            DoubleEqual,
            GreaterThanEqual,
            DoubleBar
        }
    }
}

