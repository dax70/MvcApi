namespace MvcApi.Http
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class HttpRuleParser
    {
        internal const char CR = '\r';
        private static readonly string[] dateFormats = new string[] { "ddd, d MMM yyyy H:m:s 'GMT'", "ddd, d MMM yyyy H:m:s", "d MMM yyyy H:m:s 'GMT'", "d MMM yyyy H:m:s", "ddd, d MMM yy H:m:s 'GMT'", "ddd, d MMM yy H:m:s", "d MMM yy H:m:s 'GMT'", "d MMM yy H:m:s", "dddd, d'-'MMM'-'yy H:m:s 'GMT'", "dddd, d'-'MMM'-'yy H:m:s", "ddd MMM d H:m:s yyyy", "ddd, d MMM yyyy H:m:s zzz", "ddd, d MMM yyyy H:m:s", "d MMM yyyy H:m:s zzz", "d MMM yyyy H:m:s" };
        internal static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding(0x6faf);
        internal const char LF = '\n';
        internal const int MaxInt32Digits = 10;
        internal const int MaxInt64Digits = 0x13;
        private const int maxNestedCount = 5;
        private static readonly bool[] tokenChars = new bool[0x80];

        static HttpRuleParser()
        {
            for (int i = 0x21; i < 0x7f; i++)
            {
                tokenChars[i] = true;
            }
            tokenChars[40] = false;
            tokenChars[0x29] = false;
            tokenChars[60] = false;
            tokenChars[0x3e] = false;
            tokenChars[0x40] = false;
            tokenChars[0x2c] = false;
            tokenChars[0x3b] = false;
            tokenChars[0x3a] = false;
            tokenChars[0x5c] = false;
            tokenChars[0x22] = false;
            tokenChars[0x2f] = false;
            tokenChars[0x5b] = false;
            tokenChars[0x5d] = false;
            tokenChars[0x3f] = false;
            tokenChars[0x3d] = false;
            tokenChars[0x7b] = false;
            tokenChars[0x7d] = false;
        }

        internal static bool ContainsInvalidNewLine(string value)
        {
            return ContainsInvalidNewLine(value, 0);
        }

        internal static bool ContainsInvalidNewLine(string value, int startIndex)
        {
            for (int i = startIndex; i < value.Length; i++)
            {
                if (value[i] == '\r')
                {
                    int num2 = i + 1;
                    if ((num2 < value.Length) && (value[num2] == '\n'))
                    {
                        i = num2 + 1;
                        if (i == value.Length)
                        {
                            return true;
                        }
                        char ch = value[i];
                        if ((ch != ' ') && (ch != '\t'))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static string DateToString(DateTimeOffset dateTime)
        {
            return dateTime.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);
        }

        internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
        {
            int nestedCount = 0;
            return GetExpressionLength(input, startIndex, '(', ')', true, ref nestedCount, out length);
        }

        private static HttpParseResult GetExpressionLength(string input, int startIndex, char openChar, char closeChar, bool supportsNesting, ref int nestedCount, out int length)
        {
            length = 0;
            if (input[startIndex] != openChar)
            {
                return HttpParseResult.NotParsed;
            }
            int num = startIndex + 1;
            while (num < input.Length)
            {
                int num2 = 0;
                if (((num + 2) < input.Length) && (GetQuotedPairLength(input, num, out num2) == HttpParseResult.Parsed))
                {
                    num += num2;
                    continue;
                }
                if (supportsNesting && (input[num] == openChar))
                {
                    nestedCount++;
                    try
                    {
                        if (nestedCount > 5)
                        {
                            return HttpParseResult.InvalidFormat;
                        }
                        int num3 = 0;
                        HttpParseResult result = GetExpressionLength(input, num, openChar, closeChar, supportsNesting, ref nestedCount, out num3);
                        switch (result)
                        {
                            case HttpParseResult.Parsed:
                                num += num3;
                                goto Label_0100;

                            case HttpParseResult.NotParsed:
                                Contract.Assert(false, "'NotParsed' is unexpected: We started nested expression parsing, because we found the open-char. So either it's a valid nested expression or it has invalid format.");
                                goto Label_0100;

                            case HttpParseResult.InvalidFormat:
                                return HttpParseResult.InvalidFormat;
                        }
                        Contract.Assert(false, "Unknown enum result: " + result);
                    }
                    finally
                    {
                        nestedCount--;
                    }
                }
            Label_0100:
                if (input[num] == closeChar)
                {
                    length = (num - startIndex) + 1;
                    return HttpParseResult.Parsed;
                }
                num++;
            }
            return HttpParseResult.InvalidFormat;
        }

        internal static int GetHostLength(string input, int startIndex, bool allowToken, out string host)
        {
            host = null;
            if (startIndex >= input.Length)
            {
                return 0;
            }
            int num = startIndex;
            bool flag = true;
            while (num < input.Length)
            {
                char character = input[num];
                if (character == '/')
                {
                    return 0;
                }
                if ((((character == ' ') || (character == '\t')) || (character == '\r')) || (character == ','))
                {
                    break;
                }
                flag = flag && IsTokenChar(character);
                num++;
            }
            int length = num - startIndex;
            if (length == 0)
            {
                return 0;
            }
            string str = input.Substring(startIndex, length);
            if (!((allowToken && flag) || IsValidHostName(str)))
            {
                return 0;
            }
            host = str;
            return length;
        }

        internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
        {
            int num = startIndex;
            bool flag = !allowDecimal;
            if (input[num] == '.')
            {
                return 0;
            }
            while (num < input.Length)
            {
                char ch = input[num];
                if ((ch >= '0') && (ch <= '9'))
                {
                    num++;
                }
                else if (!(flag || (ch != '.')))
                {
                    flag = true;
                    num++;
                }
                else
                {
                    break;
                }
            }
            return (num - startIndex);
        }

        internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
        {
            length = 0;
            if (input[startIndex] != '\\')
            {
                return HttpParseResult.NotParsed;
            }
            if (((startIndex + 2) > input.Length) || (input[startIndex + 1] > '\x007f'))
            {
                return HttpParseResult.InvalidFormat;
            }
            length = 2;
            return HttpParseResult.Parsed;
        }

        internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
        {
            int nestedCount = 0;
            return GetExpressionLength(input, startIndex, '"', '"', false, ref nestedCount, out length);
        }

        internal static int GetTokenLength(string input, int startIndex)
        {
            if (startIndex >= input.Length)
            {
                return 0;
            }
            for (int i = startIndex; i < input.Length; i++)
            {
                if (!IsTokenChar(input[i]))
                {
                    return (i - startIndex);
                }
            }
            return (input.Length - startIndex);
        }

        internal static int GetWhitespaceLength(string input, int startIndex)
        {
            if (startIndex >= input.Length)
            {
                return 0;
            }
            int num = startIndex;
            while (num < input.Length)
            {
                char ch = input[num];
                if ((ch == ' ') || (ch == '\t'))
                {
                    num++;
                }
                else
                {
                    if ((ch == '\r') && (((num + 2) < input.Length) && (input[num + 1] == '\n')))
                    {
                        switch (input[num + 2])
                        {
                            case ' ':
                            case '\t':
                            {
                                num += 3;
                                continue;
                            }
                        }
                    }
                    return (num - startIndex);
                }
            }
            return (input.Length - startIndex);
        }

        internal static bool IsTokenChar(char character)
        {
            if (character > '\x007f')
            {
                return false;
            }
            return tokenChars[character];
        }

        private static bool IsValidHostName(string host)
        {
            Uri uri;
            return Uri.TryCreate("http://u@" + host + "/", UriKind.Absolute, out uri);
        }

        internal static bool TryStringToDate(string input, out DateTimeOffset result)
        {
            return DateTimeOffset.TryParseExact(input, dateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out result);
        }
    }
}

