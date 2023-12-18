using AzureFunctionsRpcMessages;

namespace TestKit;

public static class BindingInfoExtensions
{
    public static bool IsMany(this BindingInfo bindingInfo)
    {
        return bindingInfo.Properties.TryGetValue("Cardinality", out var cardinality) && cardinality == "Many";
    }
}