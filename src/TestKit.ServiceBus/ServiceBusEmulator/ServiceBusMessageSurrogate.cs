using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp;

namespace TestKit.ServiceBus.ServiceBusEmulator;

#if NET7_0_OR_GREATER

[GenerateSerializer]
public struct AmqpMessageWrapper
{
    [Id(0)] public byte[] Data { get; set; }
}

[RegisterConverter]
public sealed class MyForeignLibraryValueTypeSurrogateConverter :
    Orleans.IConverter<ServiceBusMessage, AmqpMessageWrapper>
{
    public ServiceBusMessage ConvertFromSurrogate(in AmqpMessageWrapper surrogate)
    {
        var binaryData = new BinaryData(surrogate.Data.AsMemory());
        var amqpAnnotatedMessage = AmqpAnnotatedMessage.FromBytes(binaryData);
        return new ServiceBusMessage(ServiceBusReceivedMessage.FromAmqpMessage(amqpAnnotatedMessage, null));
    }

    public AmqpMessageWrapper ConvertToSurrogate(in ServiceBusMessage value)
    {
        AmqpAnnotatedMessage amqpAnnotatedMessage = value.GetRawAmqpMessage();
        var binaryData = amqpAnnotatedMessage.ToBytes();
        return new AmqpMessageWrapper
        {
            Data = binaryData.ToArray()
        };
    }
}

#endif