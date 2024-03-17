using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;

namespace TestKit.Metadata;

internal class SdkFunctionMetadata
{
    public string? Name { get; set; }

    public string? ScriptFile { get; set; }

    public string? FunctionDirectory { get; set; }

    public string? EntryPoint { get; set; }

    public string? Language { get; set; }

    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    public List<IDictionary<string, object>> Bindings { get; set; } = new List<IDictionary<string, object>>();
}