namespace TestKit.ServiceBus.ServiceBusEmulator;

#if NET7_0_OR_GREATER
[GenerateSerializer]
#endif
public class Message
{
    #if NET7_0_OR_GREATER
    [Id(0)]
    #endif
    public int Tag { get; }
}