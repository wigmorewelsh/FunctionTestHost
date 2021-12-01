using System.Collections.Generic;
using Orleans;

namespace FunctionTestHost.ServiceBusEmulator
{
    public class ServiceBusQueueGrainBase : Grain
    {
        protected Queue<Message> _queue = new();
    }
}