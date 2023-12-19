using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;

namespace TestKit.Services;

public interface IFunctionObserver : IGrainObserver
{
    Task Send(AzureFunctionsRpcMessages.StreamingMessage message);
}