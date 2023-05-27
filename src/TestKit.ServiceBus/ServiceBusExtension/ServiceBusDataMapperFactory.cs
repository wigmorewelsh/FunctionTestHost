using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;
using Orleans.Runtime;

namespace TestKit.Actors;

public class ServiceBusDataMapperFactory : IDataMapperFactory
{
    private readonly IGrainFactory _grainFactory;

    public ServiceBusDataMapperFactory(IGrainFactory grainFactory) 
    {
        _grainFactory = grainFactory;
    }

    public virtual async Task<DataMapper?> TryCreateDataMapper(FunctionLoadRequest loadRequest,
        IAddressable functionInstance)
    {
        DataMapper? dataMapper = null;
        if (TryGetServiceBusBinding(loadRequest, out var paramsSbName, out var servicebusBinding))
        {
            var isBatch = servicebusBinding.Cardinality == BindingInfo.Types.Cardinality.Many;

            //TODO: subscribe to service bus grain
            dataMapper = new ServiceBusDataMapper(isBatch, paramsSbName);
        }
        return dataMapper;
    }

    private bool TryGetServiceBusBinding(FunctionLoadRequest loadRequest, out string bindingName,
        out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Metadata.Bindings)
        {
            if (value.Type == "ServiceBusTrigger")
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