namespace MvcApi.Http
{
    using System;
    using MvcApi.Properties;

    /// <summary>A helper class for retrieving and comparing standard HTTP methods.</summary>
    public class HttpMethod : IEquatable<HttpMethod>
    {
        private string method;
        private static readonly HttpMethod deleteMethod = new HttpMethod("DELETE");
        private static readonly HttpMethod getMethod = new HttpMethod("GET");
        private static readonly HttpMethod headMethod = new HttpMethod("HEAD");
        private static readonly HttpMethod optionsMethod = new HttpMethod("OPTIONS");
        private static readonly HttpMethod postMethod = new HttpMethod("POST");
        private static readonly HttpMethod putMethod = new HttpMethod("PUT");
        private static readonly HttpMethod traceMethod = new HttpMethod("TRACE");

        public HttpMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException(SRResources.ArgumentEmptyString, "method");
            }
            if (HttpRuleParser.GetTokenLength(method, 0) != method.Length)
            {
                throw new FormatException(SRResources.HttpMethodFormatError);
            }
            this.method = method;
        }

        /// <summary>Determines whether the specified <see cref="T:HttpMethod" /> is equal to the current <see cref="T:System.Object" />.</summary>
        /// <returns>Returns <see cref="T:System.Boolean" />.true if the specified object is equal to the current object; otherwise, false.</returns>
        /// <param name="other">The HTTP method to compare with the current object.</param>
        public bool Equals(HttpMethod other)
        {
            if (other == null)
            {
                return false;
            }
            return (object.ReferenceEquals(this.method, other.method) || (string.Compare(this.method, other.method, StringComparison.OrdinalIgnoreCase) == 0));
        }

        /// <summary>Determines whether the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />.</summary>
        /// <returns>Returns <see cref="T:System.Boolean" />.true if the specified object is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object.</param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as HttpMethod);
        }

        /// <summary>Serves as a hash function for this type.</summary>
        /// <returns>Returns <see cref="T:System.Int32" />.A hash code for the current <see cref="T:System.Object" />.</returns>
        public override int GetHashCode()
        {
            return this.method.ToUpperInvariant().GetHashCode();
        }

        /// <summary>The equality operator for comparing two <see cref="T:HttpMethod" /> objects.</summary>
        /// <returns>Returns <see cref="T:System.Boolean" />.true if the specified <paramref name="left" /> and <paramref name="right" /> parameters are equal; otherwise, false.</returns>
        /// <param name="left">The left <see cref="T:HttpMethod" /> to an equality operator.</param>
        /// <param name="right">The right  <see cref="T:HttpMethod" /> to an equality operator.</param>
        public static bool operator ==(HttpMethod left, HttpMethod right)
        {
            if (left == null)
            {
                return (right == null);
            }
            if (right == null)
            {
                return (left == null);
            }
            return left.Equals(right);
        }

        /// <summary>The inequality operator for comparing two <see cref="T:HttpMethod" /> objects.</summary>
        /// <returns>Returns <see cref="T:System.Boolean" />.true if the specified <paramref name="left" /> and <paramref name="right" /> parameters are inequal; otherwise, false.</returns>
        /// <param name="left">The left <see cref="T:HttpMethod" /> to an inequality operator.</param>
        /// <param name="right">The right  <see cref="T:HttpMethod" /> to an inequality operator.</param>
        public static bool operator !=(HttpMethod left, HttpMethod right)
        {
            return !(left == right);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>Returns <see cref="T:System.String" />.A string representing the current object.</returns>
        public override string ToString()
        {
            return this.method.ToString();
        }

        /// <summary>Represents an HTTP DELETE protocol method.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Delete
        {
            get { return deleteMethod; }
        }

        /// <summary>Represents an HTTP GET protocol method.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Get
        {
            get { return getMethod; }
        }

        /// <summary>Represents an HTTP HEAD protocol method. The HEAD method is identical to GET except that the server only returns message-headers in the response, without a message-body.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Head
        {
            get { return headMethod; }
        }

        /// <summary>An HTTP method. </summary>
        /// <returns>Returns <see cref="T:System.String" />.An HTTP method represented as a <see cref="T:System.String" />.</returns>
        public string Method
        {
            get { return this.method; }
        }

        /// <summary>Represents an HTTP OPTIONS protocol method.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Options
        {
            get { return optionsMethod; }
        }

        /// <summary>Represents an HTTP POST protocol method that is used to post a new entity as an addition to a URI.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Post
        {
            get { return postMethod; }
        }

        /// <summary>Represents an HTTP PUT protocol method that is used to replace an entity identified by a URI.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Put
        {
            get { return putMethod; }
        }

        /// <summary>Represents an HTTP TRACE protocol method.</summary>
        /// <returns>Returns <see cref="T:HttpMethod" />.</returns>
        public static HttpMethod Trace
        {
            get { return traceMethod; }
        }

        public static HttpMethod GetHttpMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                return null;
            }
            if (string.Equals("GET", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Get;
            }
            if (string.Equals("POST", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Post;
            }
            if (string.Equals("PUT", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Put;
            }
            if (string.Equals("DELETE", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Delete;
            }
            if (string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Head;
            }
            if (string.Equals("OPTIONS", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Options;
            }
            if (string.Equals("TRACE", method, StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Trace;
            }
            return new HttpMethod(method);
        }
    }
}

