using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using Grpc.Core;

namespace FunctionTestHost
{
    public class FunctionMetadataService : FunctionRpc.FunctionRpcBase
    {
        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                var dd = message;
            }
        }
    }
}