using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;
using Orleans.Runtime;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.Actors;

public class ServiceBusDataMapperFactory : IDataMapperFactory
{
    private readonly IGrainFactory _grainFactory;

    public ServiceBusDataMapperFactory(IGrainFactory grainFactory) 
    {
        _grainFactory = grainFactory;
    }

    public virtual async Task<DataMapper?> TryCreateDataMapper(RpcFunctionMetadata loadRequest,
        IAddressable functionInstance)
    {
        DataMapper? dataMapper = null;
        if (TryGetServiceBusBinding(loadRequest, out var paramsSbName, out var servicebusBinding))
        {
            var isBatch = servicebusBinding.IsMany();
            // this is horrible
            var queueName = servicebusBinding.Properties["queueName"]; 

            var endpointGrain = _grainFactory.GetGrain<IServiceBusQueueGrain>(queueName);
            await endpointGrain.Subscribe(loadRequest.FunctionId, GrainExtensions.AsReference<IFunctionInstanceGrain>(functionInstance));
            dataMapper = new ServiceBusDataMapper(isBatch, paramsSbName);
        }
        return dataMapper;
    }

    private bool TryGetServiceBusBinding(RpcFunctionMetadata loadRequest, out string bindingName,
        out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Bindings)
        {
            if (value.Type.Equals("ServiceBusTrigger", StringComparison.InvariantCultureIgnoreCase))
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