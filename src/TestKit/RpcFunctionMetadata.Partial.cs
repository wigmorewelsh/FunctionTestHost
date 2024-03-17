using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace AzureFunctionsRpcMessages;

public class RawBindingInfo
{
    public Dictionary<string, string> Properties { get; init; }
}

public sealed partial class RpcFunctionMetadata
{
    public bool TryGetRawBinding(string name, [NotNullWhen(true)]out Dictionary<string, string>? rawBinding)
    {
        rawBinding = default;
        if (RawBindings.Count == 0) return false;
        foreach (var binding in RawBindings)
        {
            var bindingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(binding);
            if (bindingDict != null
                && TryGetRaw(bindingDict, out var bindingNameRaw)
                && bindingNameRaw is JsonElement { ValueKind: JsonValueKind.String } bindingNameElement
                && bindingNameElement.GetString() is { } bindingName
                && bindingName == name)
            {
                var result = new Dictionary<string, string>();
                foreach (var (key, value) in bindingDict)
                {
                    if (value is JsonElement { ValueKind: JsonValueKind.String } jsonElement)
                    {
                        result[key] = jsonElement.GetString()!;
                    }
                }
                
                rawBinding = result;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetRaw(Dictionary<string, object> bindingDict, out object? bindingNameRaw)
    {
        return bindingDict.TryGetValue("Name", out bindingNameRaw) || bindingDict.TryGetValue("name", out bindingNameRaw);
    }
}