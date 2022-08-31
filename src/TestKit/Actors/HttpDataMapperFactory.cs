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

    public virtual async Task<DataMapper?> TryCreateDataMapper(FunctionLoadRequest loadRequest, IAddressable functionInstance)
    {
        DataMapper? dataMapper = null;

        // TODO: extract this into external class and config
        if (TryGetHttpBinding(loadRequest, out var paramName, out var httpBinding))
        {
            var endpointGrain = _grainFactory.GetGrain<IFunctionEndpointGrain>(loadRequest.Metadata.Name);
            await endpointGrain.Add(loadRequest.FunctionId, GrainExtensions.AsReference<IFunctionInstanceGrain>(functionInstance));
            dataMapper = new HttpDataMapper(paramName);
        }

        return dataMapper;
    }

    private bool TryGetHttpBinding(FunctionLoadRequest loadRequest, out string bindingName, out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Metadata.Bindings)
        {
            if (value.Type == "HttpTrigger")
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