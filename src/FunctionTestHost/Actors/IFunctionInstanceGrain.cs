using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using FunctionTestHost.ServiceBusEmulator;
using Orleans;

namespace FunctionTestHost.Actors;

public interface IFunctionInstanceGrain : IGrainWithStringKey, IQueueSubscriber
{
    Task Init();
    Task InitMetadata(byte[] message);
    Task Call(string functionId);
}