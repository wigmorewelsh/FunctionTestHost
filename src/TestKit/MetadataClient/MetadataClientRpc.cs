using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionMetadataEndpoint;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TestKit.Metadata;
using FunctionRpc = FunctionMetadataEndpoint.FunctionRpc;
using StreamingMessage = FunctionMetadataEndpoint.StreamingMessage;

namespace TestKit.MetadataClient;

internal class GrpcWorkerStartupOptions
{
    public string? Host { get; set; }

    public int Port { get; set; }

    public string? WorkerId { get; set; }

    public string? RequestId { get; set; }

    public int GrpcMaxMessageLength { get; set; }
}

internal class MetadataClientRpc<TStartup> : IHostedService
{
    private readonly FunctionRpc.FunctionRpcClient _client;
    private readonly IOptions<GrpcWorkerStartupOptions> _options;

    public MetadataClientRpc(FunctionMetadataEndpoint.FunctionRpc.FunctionRpcClient client,
        IOptions<GrpcWorkerStartupOptions> options)
    {
        _client = client;
        _options = options;
    }

    public async Task UpdateMetadata()
    {
        var metadata =
            new FunctionMetadataGenerator()
                .GenerateFunctionMetadataWithReferences(typeof(TStartup).Assembly);
        var functionLoadRequests = new List<FunctionLoadRequest>();
        foreach (var sdkFunctionMetadata in metadata)
        {
            var bindingInfos = new Dictionary<string, BindingInfo>();
            foreach (IDictionary<string, object> binding in sdkFunctionMetadata.Bindings)
            {
                bindingInfos[binding["Name"] as string] = new BindingInfo
                {
                    Direction = Direction(binding),
                    Type = binding["Type"] as string,
                    DataType = DataType(binding),
                    Cardinality = Cardinality(binding)
                };
            }
            functionLoadRequests.Add(new FunctionLoadRequest
            {
                FunctionId = sdkFunctionMetadata.Name,
                Metadata = new RpcFunctionMetadata
                {
                    Name = sdkFunctionMetadata.Name,
                    // Directory = sdkFunctionMetadata.FunctionDirectory,
                    EntryPoint = sdkFunctionMetadata.EntryPoint,
                    IsProxy = false,
                    ScriptFile = sdkFunctionMetadata.ScriptFile,
                    Bindings = { bindingInfos }
                },
                ManagedDependencyEnabled = false
            });
        }

        await _client.EventStream().RequestStream.WriteAsync(new StreamingMessage
        {
            WorkerId = _options.Value.WorkerId,
            FunctionInit = new FunctionInit
            {
                FunctionLoadRequestsResults = { functionLoadRequests }
            }
        });
    }

    private static BindingInfo.Types.Direction Direction(IDictionary<string, object> binding)
    {
        if (binding.TryGetValue("Direction", out var direction))
        {
            return direction switch
            {
                "In" => BindingInfo.Types.Direction.In,
                "Out" => BindingInfo.Types.Direction.Out,
                "Inout" => BindingInfo.Types.Direction.Inout
            };
        }

        return BindingInfo.Types.Direction.In;
    }

    private static BindingInfo.Types.Cardinality Cardinality(IDictionary<string, object> binding)
    {
        if (binding.TryGetValue("Cardinality", out var cardinality))
        {
            return cardinality switch
            {
                "Many" => BindingInfo.Types.Cardinality.Many,
                "One" => BindingInfo.Types.Cardinality.One,
                _ => BindingInfo.Types.Cardinality.Unknown
            };
        }

        return BindingInfo.Types.Cardinality.Unknown;
    }

    private static BindingInfo.Types.DataType DataType(IDictionary<string, object> binding)
    {
        if (binding.TryGetValue("DataType", out var dataType))
        {
            return dataType switch
            {
                "String" => BindingInfo.Types.DataType.String,
                "Stream" => BindingInfo.Types.DataType.Stream,
                "Binary" => BindingInfo.Types.DataType.Binary,
                _ => BindingInfo.Types.DataType.Undefined
            };
        }

        return BindingInfo.Types.DataType.Undefined;
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