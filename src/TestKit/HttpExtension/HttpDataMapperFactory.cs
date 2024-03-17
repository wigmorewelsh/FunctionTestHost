using System;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;
using Orleans.Runtime;

namespace TestKit.Actors;

public class HttpDataMapperFactory : IDataMapperFactory
{
    private readonly IGrainFactory _grainFactory;

    public HttpDataMapperFactory(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public virtual async Task<DataMapper?> TryCreateDataMapper(RpcFunctionMetadata loadRequest,
        IAddressable functionInstance)
    {
        DataMapper? dataMapper = null;

        // TODO: extract this into external class and config
        if (TryGetHttpBinding(loadRequest, out var paramName, out var bindingInfo))
        {
            loadRequest.TryGetRawBinding(paramName, out var rawBinding);
            var endpointGrain = _grainFactory.GetGrain<IFunctionEndpointGrain>(loadRequest.Name);
            await endpointGrain.Add(loadRequest.FunctionId, GrainExtensions.AsReference<IFunctionInstanceGrain>(functionInstance));
            dataMapper = new HttpDataMapper(paramName, bindingInfo, rawBinding);
        }

        return dataMapper;
    }

    private bool TryGetHttpBinding(RpcFunctionMetadata loadRequest, out string bindingName, out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Bindings)
        {
            if (value.Type.Equals("HttpTrigger", StringComparison.InvariantCultureIgnoreCase))
            {
                bindingName = key;
                bindingInfo = value;
                return true;
            }
        }

        bindingName = null;
        bindingInfo = null;
        return false;
    }
}