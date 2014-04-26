using System;
using System.Collections.ObjectModel;

namespace MvcApi.Schema
{
    public class SchemaCollection
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public long timestamp { get; set; }
        public Collection<SchemaRequest> requests { get; set; }
    }
}
