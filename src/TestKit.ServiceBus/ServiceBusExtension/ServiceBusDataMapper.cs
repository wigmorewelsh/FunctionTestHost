using AzureFunctionsRpcMessages;

namespace TestKit.Actors;

public class ServiceBusDataMapper : DataMapper
{
    private readonly bool _isBatch;

    public ServiceBusDataMapper(bool isBatch, string paramName) : base(paramName)
    {
        _isBatch = isBatch;
    }

    public override TypedData ToTypedData(string functionId, RpcHttp body)
    {
        var typedData = new TypedData();
        if (_isBatch)
        {
            var coll = new CollectionBytes();
            coll.Bytes.Add(body.Body.Bytes);
            typedData.CollectionBytes = coll;
        }
        else
        {
            typedData.Bytes = body.Body.Bytes;
        }

        return typedData;
    }
}