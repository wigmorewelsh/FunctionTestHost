using TestKit.TestHost;

namespace TestKit.ServiceBus;

public class ServiceBusTestHost : FunctionTestHost
{
    public ServiceBusTestHost()
    {
        this.AddServiceBusTestHost();
    }
}