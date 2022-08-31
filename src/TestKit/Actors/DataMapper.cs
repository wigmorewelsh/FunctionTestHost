using AzureFunctionsRpcMessages;

namespace TestKit.Actors;

public abstract class DataMapper
{
    public string ParamsName { get; }

    protected DataMapper(string paramName)
    {
        ParamsName = paramName;
    }

    public abstract TypedData ToTypedData(string functionId, RpcHttp body);
}