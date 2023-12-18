using System;
using AzureFunctionsRpcMessages;

namespace TestKit.Actors;

public class HttpDataMapper : DataMapper
{
    private readonly BindingInfo _bindingInfo;

    public HttpDataMapper(string bindingName, BindingInfo bindingInfo) : base(bindingName)
    {
        _bindingInfo = bindingInfo;
    }

    public override TypedData ToTypedData(string functionId, RpcHttp body)
    {
        if (_bindingInfo.DataType == BindingInfo.Types.DataType.Binary && !_bindingInfo.IsMany())
        {
            if (body.Body.Bytes is { } bytes)
            {
                return new TypedData
                {
                    Bytes = bytes
                };  
            } 
        }
        if (_bindingInfo.DataType == BindingInfo.Types.DataType.Binary && _bindingInfo.IsMany())
        {
            if (body.Body.Bytes is { } bytes)
            {
                return new TypedData
                {
                    CollectionBytes = new CollectionBytes()
                    {
                        Bytes = { bytes }
                    }
                };  
            } 
        }
        if (_bindingInfo.DataType == BindingInfo.Types.DataType.String)
        {
            if (body.Body.String is { } str)
            {
                return new TypedData
                {
                    String = str
                }; 
            } 
        }
        
        return new TypedData
        {
            Http = body
        };
     

        throw new NotImplementedException();
    }
}