#if NET6_0

using System;
using System.IO;
using Google.Protobuf;
using Orleans.Serialization;

namespace TestKit.TestHost;

public class ProtobufNetSerializer : IExternalSerializer
{
    public bool IsSupportedType(Type itemType)
    {
        return itemType.IsAssignableTo(typeof(IMessage));
    }

    public object DeepCopy(object source, ICopyContext context)
    {
        if (source == null)
        {
            return null;
        }

        dynamic self = this;
        dynamic message = source;
        return self.DeepCopyImpl(message);
    }

    private T DeepCopyImpl<T>(T message) where T : IDeepCloneable<T>
    {
        return message.Clone();
    }

    public void Serialize(object item, ISerializationContext context, Type expectedType)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (item == null)
        {
            // Special handling for null value.
            // Since in this ProtobufSerializer we are usually writing the data lengh as 4 bytes
            // we also have to write the Null object as 4 bytes lengh of zero.
            context.StreamWriter.Write(0);
            return;
        }

        var message = item as IMessage;
        var ms = new MemoryStream();
        message.WriteTo(ms);
        ms.Position = 0;
        context.StreamWriter.Write(ms.ToArray());
    }

    public object Deserialize(Type expectedType, IDeserializationContext context)
    {
        if (expectedType == null)
        {
            throw new ArgumentNullException(nameof(expectedType));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var reader = context.StreamReader;
        int length = reader.ReadInt();
        byte[] data = reader.ReadBytes(length);

        object message = null;
        using (var stream = new MemoryStream(data))
        {
            var obj = Activator.CreateInstance(expectedType) as IMessage;
            obj.MergeFrom(stream);
            message = obj;
        }

        return message;
    }
}
#endif
