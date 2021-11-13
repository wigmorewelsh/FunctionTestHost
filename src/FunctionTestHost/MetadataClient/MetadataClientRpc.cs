using System.Threading;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunctionTestHost.MetadataClient
{
    public class GrpcWorkerStartupOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int GrpcMaxMessageLength { get; set; }
    }
    
    public class MetadataClientRpc : IHostedService
    {
        private readonly FunctionRpc.FunctionRpcClient _client;
        private readonly IOptions<GrpcWorkerStartupOptions> _options;

        public MetadataClientRpc(FunctionMetadataEndpoint.FunctionRpc.FunctionRpcClient client, IOptions<GrpcWorkerStartupOptions> options)
        {
            _client = client;
            _options = options;
        }

        public async Task UpdateMetadata()
        {
            await _client.EventStream().RequestStream.WriteAsync(new StreamingMessage
            {
               Ping = new Ping
               {
                   WorkerId = _options.Value.WorkerId
               }
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateMetadata();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}