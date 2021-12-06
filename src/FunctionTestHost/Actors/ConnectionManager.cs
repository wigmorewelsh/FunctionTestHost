using System.Collections.Concurrent;
using System.Threading.Channels;
using AzureFunctionsRpcMessages;

namespace FunctionTestHost.Actors
{
    public class ConnectionManager
    {
        ConcurrentDictionary<string, Channel<StreamingMessage>> _channels = new ();

        public ChannelReader<StreamingMessage> Init(string name)
        {
            return _channels.GetOrAdd(name, _ => Channel.CreateUnbounded<StreamingMessage>());
        }

        public ChannelWriter<StreamingMessage> Lookup(string name)
        {
            return _channels.GetOrAdd(name, _ => Channel.CreateUnbounded<StreamingMessage>());
        }
    }
}