namespace MvcApi
{
    using System;
    using System.Collections;

    public interface IVirtualList
    {
        IEnumerable Results { get; set; }

        int TotalCount { get; set; }
    }
}
