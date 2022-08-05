using System.Collections.Generic;
using Orleans;

namespace TestKit.ServiceBusEmulator;

public class ServiceBusQueueGrainBase : Grain
{
    protected Queue<Message> _queue = new();
}