namespace MvcApi.Formatting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Enumerable conveys the meaning of collection")]
    public sealed class DelegatingEnumerable<T> : IEnumerable<T>, IEnumerable
    {
        private IEnumerable<T> source;

        public DelegatingEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "Required by XmlSerializer, never used.")]
        public void Add(object item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.source.GetEnumerator();
        }
    }
}
