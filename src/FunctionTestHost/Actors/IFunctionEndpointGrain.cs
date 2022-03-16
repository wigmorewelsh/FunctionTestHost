using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;

namespace FunctionTestHost.Actors;

public interface IFunctionEndpointGrain : IPublicEndpoint, IGrainWithStringKey
{
    Task Add(IFunctionInstanceGrain functionInstanceGrain);
}