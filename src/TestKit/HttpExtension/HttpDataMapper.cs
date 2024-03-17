using System;
using System.Collections.Generic;
using AzureFunctionsRpcMessages;

namespace TestKit.Actors;

public class HttpDataMapper : DataMapper
{
    private readonly BindingInfo _bindingInfo;
    private readonly Dictionary<string, string>? _rawBindingInfo;

    public HttpDataMapper(string bindingName, BindingInfo bindingInfo, Dictionary<string, string>? rawBindingInfo) : base(bindingName)
    {
        _bindingInfo = bindingInfo;
        _rawBindingInfo = rawBindingInfo;
    }

    public override TypedData ToTypedData(string functionId, RpcHttp body)
    {
        if (_bindingInfo.DataType == BindingInfo.Types.DataType.Binary && !_rawBindingInfo.IsMany())
        {
            if (body.Body.Bytes is { } bytes)
            {
                return new TypedData
                {
                    Bytes = bytes
                };  
            } 
        }
        if (_bindingInfo.DataType == BindingInfo.Types.DataType.Binary && _rawBindingInfo.IsMany())
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