namespace MvcApi
{
    using System;

    public static class HttpMethods
    {
        static HttpMethods()
        {
            Get = "GET";
            Post = "POST";
            Put = "PUT";
            Delete = "DELETE";
            Head = "HEAD";
            Options = "OPTIONS";
            Trace = "TRACE";
            Patch = "PATCH";

            AllowedVerbs = new string[] { Get, Post, Put, Delete };

            AllVerbs = new string[] { Get, Post, Put, Delete, Delete, Head, Options, Trace, Patch };
        }

        public static readonly string Delete;
        public static readonly string Get;
        public static readonly string Head;
        public static readonly string Options;
        public static readonly string Post;
        public static readonly string Put;
        public static readonly string Trace;
        public static readonly string Patch;

        public static readonly string[] AllowedVerbs;

        public static readonly string[] AllVerbs;

    }
}
