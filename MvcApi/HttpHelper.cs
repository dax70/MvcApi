//----------------------------------------------------------------
// Originally from Mvc Rest Futures
//----------------------------------------------------------------

namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Mime;
    using System.Web;
    using MvcApi.Formatting; 
    #endregion

    static class HttpHelper
    {        
        /// <summary>
        /// Returns the content type of the request, based on the Content-Type header
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static ContentType GetRequestFormat(HttpRequestBase request)
        {
            if (!string.IsNullOrEmpty(request.ContentType))
            {
                try
                {
                    return new ContentType(request.ContentType);
                }
                catch (FormatException)
                {
                    throw new HttpException((int)HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type: '" + request.ContentType + "'");
                }
            }
            // ContentType defaults to "application/octet-stream" per RFC 2616 7.2.1
            return new ContentType();
        }

        /// <summary>
        /// Returns the preferred content type to use for the response, based on the request, according to the following
        /// rules:
        /// 1. If the query string contains a key called "format", its value is returned as the content type
        /// 2. Otherwise, if the request has an Accepts header, the list of content types in order of preference is returned
        /// 3. Otherwise, if the request has a content type, its value is returned
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<ContentType> GetResponseFormats(HttpRequestBase request)
        {
            ContentType contentType;
            if (HttpHelper.TryGetFromUri(request, out contentType))
            {
                return new List<ContentType>(new ContentType[] { contentType });
            }
            string[] accepts = request.AcceptTypes;
            if (accepts != null && accepts.Length > 0)
            {
                return HttpHelper.GetAcceptHeaderElements(accepts);
            }
            contentType = HttpHelper.GetRequestFormat(request);
            return new List<ContentType>(new ContentType[] { contentType });
        }

        static List<ContentType> GetAcceptHeaderElements(string[] acceptHeaderElements)
        {
            List<ContentType> contentTypeList = new List<ContentType>(acceptHeaderElements.Length);
            foreach (string acceptHeaderElement in acceptHeaderElements)
            {
                try
                {
                    ContentType contentType = new ContentType(acceptHeaderElement);
                    contentTypeList.Add(contentType);
                }
                catch (FormatException)
                {
                    // ignore unknown formats to allow fallback
                }
            }
            contentTypeList.Sort(new AcceptHeaderElementComparer());
            return contentTypeList;
        }

        // URI-based format override, useful for testing from the browser
        static bool TryGetFromUri(HttpRequestBase request, out ContentType contentType)
        {
            bool isBrowserRequest = false;
            try
            {
                isBrowserRequest = request.IsBrowserRequest();
            }
            catch (HttpException)
            {
                // IsBrowserRequest will throw HttpException if the request has an
                // unrecognized ContentType, it makes sense to swallow that exception here
            }
            if (isBrowserRequest)
            {
                string fromParams = request.QueryString["format"];
                if (!string.IsNullOrEmpty(fromParams))
                {
                    try
                    {
                        contentType = new ContentType(fromParams);
                        return true;
                    }
                    catch (FormatException)
                    {
                        // This may be a friendly name (for example, "xml" instead of "text/xml").
                        // if so, try mapping to a content type
                        //if (FormatManager.Current.TryMapFormatFriendlyName(fromParams, out contentType))
                        //{
                        //    return true;
                        //}
                    }
                }
            }
            contentType = null;
            return false;
        }

        class AcceptHeaderElementComparer : IComparer<ContentType>
        {
            public int Compare(ContentType x, ContentType y)
            {
                string[] xTypeSubType = x.MediaType.Split('/');
                string[] yTypeSubType = y.MediaType.Split('/');

                if (string.Equals(xTypeSubType[0], yTypeSubType[0], StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(xTypeSubType[1], yTypeSubType[1], StringComparison.OrdinalIgnoreCase))
                    {
                        // need to check the number of parameters to determine which is more specific
                        bool xHasParam = AcceptHeaderElementComparer.HasParameters(x);
                        bool yHasParam = AcceptHeaderElementComparer.HasParameters(y);
                        if (xHasParam && !yHasParam)
                        {
                            return 1;
                        }
                        else if (!xHasParam && yHasParam)
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        if (xTypeSubType[1][0] == '*' && xTypeSubType[1].Length == 1)
                        {
                            return 1;
                        }
                        if (yTypeSubType[1][0] == '*' && yTypeSubType[1].Length == 1)
                        {
                            return -1;
                        }
                    }
                }
                else if (xTypeSubType[0][0] == '*' && xTypeSubType[0].Length == 1)
                {
                    return 1;
                }
                else if (yTypeSubType[0][0] == '*' && yTypeSubType[0].Length == 1)
                {
                    return -1;
                }

                decimal qualityDifference = AcceptHeaderElementComparer.GetQualityFactor(x) - AcceptHeaderElementComparer.GetQualityFactor(y);
                if (qualityDifference < 0)
                {
                    return 1;
                }
                else if (qualityDifference > 0)
                {
                    return -1;
                }
                return 0;
            }

            static decimal GetQualityFactor(ContentType contentType)
            {
                decimal result;
                foreach (string key in contentType.Parameters.Keys)
                {
                    if (string.Equals("q", key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (decimal.TryParse(contentType.Parameters[key], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result) &&
                            (result <= (decimal)1.0))
                        {
                            return result;
                        }
                    }
                }

                return (decimal)1.0;
            }

            static bool HasParameters(ContentType contentType)
            {
                int number = 0;
                foreach (string param in contentType.Parameters.Keys)
                {
                    if (!string.Equals("q", param, StringComparison.OrdinalIgnoreCase))
                    {
                        number++;
                    }
                }

                return (number > 0);
            }
        }
    }
}
