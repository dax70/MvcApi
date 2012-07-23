namespace MvcApi.Http
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Net.Http.Headers;

    internal class DictionaryAdapterCollection : Collection<NameValueHeaderValue>
    {
        private StringDictionary source;

        internal DictionaryAdapterCollection(StringDictionary source)
        {
            this.source = source;
        }

        protected override void ClearItems()
        {
            this.source.Clear();
            base.ClearItems();
        }

        protected override void InsertItem(int index, NameValueHeaderValue item)
        {
            this.source.Add(item.Name, item.Value);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            this.source.Remove(item.Name);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, NameValueHeaderValue item)
        {
            this.source[item.Name] = item.Value;
            base.SetItem(index, item);
        }

    }
}
