// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
namespace MvcApi.Query
{
    /// <summary>
    /// A <see cref="IStructuredQuerySource"/> is used to extract the query from a Uri.
    /// </summary>
    public interface IStructuredQuerySource
    {
        /// <summary>
        /// Create the <see cref="StructuredQuery"/> for the given uri. Return null if there is no query 
        /// in the Uri.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to build the <see cref="StructuredQuery"/> from</param>
        /// <returns>The <see cref="StructuredQuery"/></returns>
        StructuredQuery CreateQuery(Uri uri);
    }
}
