using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Web.Http.Dispatcher;

namespace MvcApi.OData
{
    internal static class TypeHelper
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching all exceptions in this case is the right to do.")]
        // This code is copied from DefaultHttpControllerTypeResolver.GetControllerTypes.
        internal static IEnumerable<Type> GetLoadedTypes(IAssembliesResolver assembliesResolver)
        {
            List<Type> result = new List<Type>();

            // Go through all assemblies referenced by the application and search for types matching a predicate
            ICollection<Assembly> assemblies = assembliesResolver.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] exportedTypes = null;
                if (assembly == null || assembly.IsDynamic)
                {
                    // can't call GetExportedTypes on a dynamic assembly
                    continue;
                }

                try
                {
                    exportedTypes = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    exportedTypes = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (exportedTypes != null)
                {
                    result.AddRange(exportedTypes.Where(t => t != null));
                }
            }

            return result;
        }
    }
}
