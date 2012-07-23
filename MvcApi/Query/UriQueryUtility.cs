namespace MvcApi.Query
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.Text;

    internal static class UriQueryUtility
    {
        #region Nested Types

        [Serializable]
        internal class HttpValueCollection : NameValueCollection
        {
            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Ported from WCF")]
            internal HttpValueCollection(string str)
                : base(StringComparer.OrdinalIgnoreCase)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    this.FillFromString(str, true);
                }
                base.IsReadOnly = false;
            }

            protected HttpValueCollection(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public override string ToString()
            {
                return this.ToString(true, null);
            }

            internal void FillFromString(string s, bool urlencoded)
            {
                int num = (s != null) ? s.Length : 0;
                for (int i = 0; i < num; i++)
                {
                    int startIndex = i;
                    int num4 = -1;
                    while (i < num)
                    {
                        char ch = s[i];
                        if (ch == '=')
                        {
                            if (num4 < 0)
                            {
                                num4 = i;
                            }
                        }
                        else if (ch == '&')
                        {
                            break;
                        }
                        i++;
                    }
                    string str = string.Empty;
                    string str2 = string.Empty;
                    if (num4 >= 0)
                    {
                        str = s.Substring(startIndex, num4 - startIndex);
                        str2 = s.Substring(num4 + 1, (i - num4) - 1);
                    }
                    else
                    {
                        str2 = s.Substring(startIndex, i - startIndex);
                    }
                    if (urlencoded)
                    {
                        this.Add(UriQueryUtility.UrlDecode(str), UriQueryUtility.UrlDecode(str2));
                    }
                    else
                    {
                        this.Add(str, str2);
                    }
                    if ((i == (num - 1)) && (s[i] == '&'))
                    {
                        this.Add(string.Empty, string.Empty);
                    }
                }
            }
            private string ToString(bool urlencoded, IDictionary excludeKeys)
            {
                int count = this.Count;
                if (count == 0)
                {
                    return string.Empty;
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    string key = this.GetKey(i);
                    if (((excludeKeys == null) || (key == null)) || (excludeKeys[key] == null))
                    {
                        string str3;
                        if (urlencoded)
                        {
                            key = UriQueryUtility.UrlEncode(key);
                        }
                        string str2 = !string.IsNullOrEmpty(key) ? (key + "=") : string.Empty;
                        ArrayList list = (ArrayList)base.BaseGet(i);
                        int num3 = (list != null) ? list.Count : 0;
                        if (builder.Length > 0)
                        {
                            builder.Append('&');
                        }
                        if (num3 == 1)
                        {
                            builder.Append(str2);
                            str3 = (string)list[0];
                            if (urlencoded)
                            {
                                str3 = UriQueryUtility.UrlEncode(str3);
                            }
                            builder.Append(str3);
                        }
                        else if (num3 == 0)
                        {
                            builder.Append(str2);
                        }
                        else
                        {
                            for (int j = 0; j < num3; j++)
                            {
                                if (j > 0)
                                {
                                    builder.Append('&');
                                }
                                builder.Append(str2);
                                str3 = (string)list[j];
                                if (urlencoded)
                                {
                                    str3 = UriQueryUtility.UrlEncode(str3);
                                }
                                builder.Append(str3);
                            }
                        }
                    }
                }
                return builder.ToString();
            }
        }

        private class UrlDecoder
        {
            // Fields
            private int _bufferSize;
            private byte[] _byteBuffer;
            private char[] _charBuffer;
            private Encoding _encoding;
            private int _numBytes;
            private int _numChars;

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                this._bufferSize = bufferSize;
                this._encoding = encoding;
                this._charBuffer = new char[bufferSize];
            }

            internal void AddByte(byte b)
            {
                if (this._byteBuffer == null)
                {
                    this._byteBuffer = new byte[this._bufferSize];
                }
                this._byteBuffer[this._numBytes++] = b;
            }

            internal void AddChar(char ch)
            {
                if (this._numBytes > 0)
                {
                    this.FlushBytes();
                }
                this._charBuffer[this._numChars++] = ch;
            }

            private void FlushBytes()
            {
                if (this._numBytes > 0)
                {
                    this._numChars += this._encoding.GetChars(this._byteBuffer, 0, this._numBytes, this._charBuffer, this._numChars);
                    this._numBytes = 0;
                }
            }

            internal string GetString()
            {
                if (this._numBytes > 0)
                {
                    this.FlushBytes();
                }
                if (this._numChars > 0)
                {
                    return new string(this._charBuffer, 0, this._numChars);
                }
                return string.Empty;
            }
        }
        #endregion

        public static NameValueCollection ParseQueryString(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if ((query.Length > 0) && (query[0] == '?'))
            {
                query = query.Substring(1);
            }
            return new HttpValueCollection(query);
        }

        public static string UrlDecode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeInternal(str, Encoding.UTF8);
        }

        public static string UrlEncode(string str)
        {
            if (str == null)
            {
                return null;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Encoding.ASCII.GetString(UrlEncode(bytes, 0, bytes.Length, false));
        }

        private static bool IsUrlSafeChar(char ch)
        {
            if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
            {
                return true;
            }
            switch (ch)
            {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }
            return false;
        }

        private static int HexToInt(char h)
        {
            if ((h >= '0') && (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }

        private static string UrlDecodeInternal(string value, Encoding encoding)
        {
            if (value == null)
            {
                return null;
            }
            int count = value.Length;
            UrlDecoder decoder = new UrlDecoder(count, encoding);
            for (int pos = 0; pos < count; pos++)
            {
                char ch = value[pos];
                if (ch == '+')
                {
                    ch = ' ';
                }
                else if ((ch == '%') && (pos < (count - 2)))
                {
                    if (value[pos + 1] == 'u' && pos < (count - 5))
                    {
                        int h1 = HexToInt(value[pos + 2]);
                        int h2 = HexToInt(value[pos + 3]);
                        int h3 = HexToInt(value[pos + 4]);
                        int h4 = HexToInt(value[pos + 5]);
                        
                        if (((h1 < 0) || (h2 < 0)) || ((h3 < 0) || (h4 < 0)))
                        {
                            goto Label_010B;
                        }
                        ch = (char)((((h1 << 12) | (h2 << 8)) | (h3 << 4)) | h4);
                        pos += 5;
                        decoder.AddChar(ch);
                        continue;
                    }
                    int num7 = HexToInt(value[pos + 1]);
                    int num8 = HexToInt(value[pos + 2]);
                    if ((num7 >= 0) && (num8 >= 0))
                    {
                        byte b = (byte)((num7 << 4) | num8);
                        pos += 2;
                        decoder.AddByte(b);
                        continue;
                    }
                }
            Label_010B:
                if ((ch & 0xff80) == 0)
                {
                    decoder.AddByte((byte)ch);
                }
                else
                {
                    decoder.AddChar(ch);
                }
            }
            return decoder.GetString();
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            byte[] buffer = UrlEncode(bytes, offset, count);
            if ((alwaysCreateNewReturnValue && (buffer != null)) && (buffer == bytes))
            {
                return (byte[])buffer.Clone();
            }
            return buffer;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];
                if (ch == ' ')
                {
                    num++;
                }
                else if (!IsUrlSafeChar(ch))
                {
                    num2++;
                }
            }
            if ((num == 0) && (num2 == 0))
            {
                return bytes;
            }
            byte[] buffer = new byte[count + (num2 * 2)];
            int num4 = 0;
            for (int j = 0; j < count; j++)
            {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsUrlSafeChar(ch2))
                {
                    buffer[num4++] = num6;
                }
                else if (ch2 == ' ')
                {
                    buffer[num4++] = 0x2b;
                }
                else
                {
                    buffer[num4++] = 0x25;
                    buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
                    buffer[num4++] = (byte)IntToHex(num6 & 15);
                }
            }
            return buffer;
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return false;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return true;
        }

    }
}
