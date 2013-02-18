using System;
using System.Diagnostics.Contracts;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using Microsoft.Data.Edm;

namespace MvcApi.OData
{
    internal static class ActionDescriptorExtensions
    {
        internal const string EdmModelKey = "MS_EdmModel";

        internal static IEdmModel GetEdmModel(this ApiActionDescriptor actionDescriptor, Type entityClrType)
        {
            if (actionDescriptor == null)
            {
                throw Error.ArgumentNull("actionDescriptor");
            }

            if (entityClrType == null)
            {
                throw Error.ArgumentNull("entityClrType");
            }

            // save the EdmModel to the action descriptor
            return actionDescriptor.Properties.GetOrAdd(EdmModelKey + entityClrType.FullName, _ =>
            {
                // It's safe to create HttpConfiguration, since it's used as an assembly Resolver.
                //ODataConventionModelBuilder builder = new ODataConventionModelBuilder(new HttpConfiguration(), isQueryCompositionMode: true);
                ODataConventionModelBuilder builder = new ODataConventionModelBuilder(new HttpConfiguration());                
                EntityTypeConfiguration entityTypeConfiguration = builder.AddEntity(entityClrType);
                builder.AddEntitySet(entityClrType.Name, entityTypeConfiguration);
                IEdmModel edmModel = builder.GetEdmModel();
                Contract.Assert(edmModel != null);
                return edmModel;
            }) as IEdmModel;
        }
    }
}
