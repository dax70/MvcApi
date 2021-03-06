﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

namespace MvcApi.Formatting
{
    internal enum MediaTypeHeaderValueRange
    {
        /// <summary>
        /// Not a media type range
        /// </summary>
        None = 0,

        /// <summary>
        /// A subtype media range, e.g. "application/*".
        /// </summary>
        SubtypeMediaRange,

        /// <summary>
        /// An all media range, e.g. "*/*".
        /// </summary>
        AllMediaRange,
    }
}
