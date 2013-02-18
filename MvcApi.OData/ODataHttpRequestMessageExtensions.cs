using System;
using Microsoft.Data.Edm;
using MvcApi.Http;

namespace MvcApi.OData
{
    public static class ODataHttpRequestMessageExtensions
    {
        private const string EdmModelKey = "MS_EdmModel";

        /// <summary>
        /// Retrieves the EDM model associated with the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The EDM model associated with this request, or <c>null</c> if there isn't one.</returns>
        public static IEdmModel GetEdmModel(this HttpRequestMessage request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            object model;
            if (request.Properties.TryGetValue(EdmModelKey, out model))
            {
                return model as IEdmModel;
            }

            return null;
        }
    }
}
