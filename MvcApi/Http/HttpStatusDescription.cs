namespace MvcApi.Http
{
    using System;
    using System.Net;

    internal static class HttpStatusDescription
    {
        private static readonly string[][] httpStatusDescriptions;

        static HttpStatusDescription()
        {
            string[][] strArray = new string[6][];
            strArray[1] = new string[] { "Continue", "Switching Protocols", "Processing" };
            strArray[2] = new string[] { "OK", "Created", "Accepted", "Non-Authoritative Information", "No Content", "Reset Content", "Partial Content", "Multi-Status" };
            string[] strArray2 = new string[8];
            strArray2[0] = "Multiple Choices";
            strArray2[1] = "Moved Permanently";
            strArray2[2] = "Found";
            strArray2[3] = "See Other";
            strArray2[4] = "Not Modified";
            strArray2[5] = "Use Proxy";
            strArray2[7] = "Temporary Redirect";
            strArray[3] = strArray2;
            strArray2 = new string[0x1a];
            strArray2[0] = "Bad Request";
            strArray2[1] = "Unauthorized";
            strArray2[2] = "Payment Required";
            strArray2[3] = "Forbidden";
            strArray2[4] = "Not Found";
            strArray2[5] = "Method Not Allowed";
            strArray2[6] = "Not Acceptable";
            strArray2[7] = "Proxy Authentication Required";
            strArray2[8] = "Request Timeout";
            strArray2[9] = "Conflict";
            strArray2[10] = "Gone";
            strArray2[11] = "Length Required";
            strArray2[12] = "Precondition Failed";
            strArray2[13] = "Request Entity Too Large";
            strArray2[14] = "Request-Uri Too Long";
            strArray2[15] = "Unsupported Media Type";
            strArray2[0x10] = "Requested Range Not Satisfiable";
            strArray2[0x11] = "Expectation Failed";
            strArray2[0x16] = "Unprocessable Entity";
            strArray2[0x17] = "Locked";
            strArray2[0x18] = "Failed Dependency";
            strArray[4] = strArray2;
            strArray2 = new string[8];
            strArray2[0] = "Internal Server Error";
            strArray2[1] = "Not Implemented";
            strArray2[2] = "Bad Gateway";
            strArray2[3] = "Service Unavailable";
            strArray2[4] = "Gateway Timeout";
            strArray2[5] = "Http Version Not Supported";
            strArray2[7] = "Insufficient Storage";
            strArray[5] = strArray2;
            httpStatusDescriptions = strArray;
        }

        internal static string Get(int code)
        {
            if ((code >= 100) && (code < 600))
            {
                int index = code / 100;
                int num2 = code % 100;
                if (num2 < httpStatusDescriptions[index].Length)
                {
                    return httpStatusDescriptions[index][num2];
                }
            }
            return null;
        }

        internal static string Get(HttpStatusCode code)
        {
            return Get((int) code);
        }
    }
}

