using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MvcApi.Schema
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class SchemaController : ApiController
    {
        public SchemaCollection Get()
        {
            //var collection = Configuration.Properties.GetOrAdd("postmanCollection", k =>
            //    {
            var requestUri = Request.Url;
            string baseUri = requestUri.Scheme + "://" + requestUri.Host + ":" + requestUri.Port + HttpContext.Request.ApplicationPath;
            var postManCollection = new SchemaCollection();

            postManCollection.id = Guid.NewGuid();

            postManCollection.name = "Exposed Endpoints"; // TODO: Make configurable.

            postManCollection.timestamp = DateTime.Now.Ticks;

            postManCollection.requests = new Collection<SchemaRequest>();

            var actionDescriptors = new ApiActionDescriptor[0]; //Configuration.Services.GetApiExplorer().ApiDescriptions;
            foreach (var apiDescription in actionDescriptors)
            {
                var request = new SchemaRequest
                {
                    collectionId = postManCollection.id,
                    id = Guid.NewGuid(),
                    method = Request.RequestContext.GetHttpMethod(),
                    //url = baseUri.TrimEnd('/') + "/" + apiDescription.RelativePath,
                    //description = apiDescription.Documentation,
                    //name = apiDescription.RelativePath,
                    data = "",
                    headers = "",
                    dataMode = "params",
                    timestamp = 0
                };

                postManCollection.requests.Add(request);
            }

            //}) as SchemaCollection;

            Request.RequestContext.HttpContext.Response.StatusCode = 200; //HtttpStatusCode.Ok
            //return Request.CreateResponse<PostmanCollection>(HttpStatusCode.OK, collection, "application/json");
            return postManCollection;
        }
    }
}
