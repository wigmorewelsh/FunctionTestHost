using System.Threading.Tasks;
using Orleans.Runtime;

namespace FunctionTestHost.ServiceBusEmulator;

public interface IQueueSubscriber : IAddressable
{
    Task Notification();
}