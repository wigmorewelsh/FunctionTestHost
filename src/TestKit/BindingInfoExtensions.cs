using System.Collections.Generic;
using AzureFunctionsRpcMessages;

namespace TestKit;

public static class BindingInfoExtensions
{
    public static bool IsMany(this BindingInfo bindingInfo)
    {
        return bindingInfo.Properties.TryGetValue("Cardinality", out var cardinality) && cardinality == "Many";
    }

    public static bool IsMany(this Dictionary<string, string>? rawBindingInfo)
    {
        if (rawBindingInfo == null) return false;
        return (rawBindingInfo.TryGetValue("Cardinality", out var cardinality)
                || rawBindingInfo.TryGetValue("cardinality", out cardinality))
               && cardinality == "Many";
    }
}