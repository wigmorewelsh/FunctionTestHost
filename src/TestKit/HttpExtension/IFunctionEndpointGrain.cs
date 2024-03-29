using System.Threading.Tasks;
using Orleans;

namespace TestKit.Actors;

public interface IFunctionEndpointGrain : IPublicEndpoint, IGrainWithStringKey
{
    Task Add(string functionId, IFunctionInstanceGrain functionInstanceGrain);
}