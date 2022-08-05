using System.Threading.Tasks;
using Orleans.Runtime;

namespace TestKit.ServiceBusEmulator;

public interface IQueueSubscriber : IAddressable
{
    Task Notification();
}