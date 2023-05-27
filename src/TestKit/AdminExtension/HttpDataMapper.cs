using AzureFunctionsRpcMessages;

namespace TestKit.Actors;

public class AdminHttpDataMapper : DataMapper
{
    public AdminHttpDataMapper(string bindingName) : base(bindingName)
    {
    }

    public override TypedData ToTypedData(string functionId, RpcHttp body)
    {
        var typedData = new TypedData
        {
            Http = body
        };
        return typedData;
    }
}