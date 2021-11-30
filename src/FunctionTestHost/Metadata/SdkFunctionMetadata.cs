using System.Collections.Generic;
using System.Dynamic;
using Mono.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class SdkFunctionMetadata
    {
        public string? Name { get; set; }

        public string? ScriptFile { get; set; }

        public string? FunctionDirectory { get; set; }

        public string? EntryPoint { get; set; }

        public string? Language { get; set; }

        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public Collection<ExpandoObject> Bindings { get; set; } = new Collection<ExpandoObject>();
    }
}