using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionMetadataEndpoint;
using Google.Protobuf.Collections;
using Grpc.Core;
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

internal class MetadataClientRpc<TStartup> : BackgroundService 
{
    private readonly FunctionRpc.FunctionRpcClient _client;
    private readonly IOptions<GrpcWorkerStartupOptions> _options;

    public MetadataClientRpc(FunctionMetadataEndpoint.FunctionRpc.FunctionRpcClient client,
        IOptions<GrpcWorkerStartupOptions> options)
    {
        _client = client;
        _options = options;
    }

    public async Task UpdateMetadata(CancellationToken stoppingToken)
    {
        var stream = _client.EventStream();
        await stream.RequestStream.WriteAsync(new StreamingMessage
        {
            StartStream = new StartStream
            {
                WorkerId = _options.Value.WorkerId,
            }
        });

        await foreach (var message in stream.ResponseStream.ReadAllAsync(stoppingToken))
        {
            if (message.FunctionsMetadataRequest is { } metadataRequest)
            {
                await LoadFunctionMetadata(stream.RequestStream);
            }
        }
    }

    private async Task LoadFunctionMetadata(IClientStreamWriter<StreamingMessage> streamRequestStream)
    {
        var metadata =
            new FunctionMetadataGenerator()
                .GenerateFunctionMetadataWithReferences(typeof(TStartup).Assembly);
        var functionLoadRequests = new List<RpcFunctionMetadata>();
        foreach (var sdkFunctionMetadata in metadata)
        {
            var bindingInfos = new Dictionary<string, BindingInfo>();
            foreach (IDictionary<string, object> binding in sdkFunctionMetadata.Bindings)
            {
                var map = new MapField<string, string>();
                foreach (var (key, value) in binding)
                {
                    if (value is string str)
                    {
                        map.Add(key, str);
                    }
                }

                bindingInfos[binding["Name"] as string] = new BindingInfo
                {
                    Direction = Direction(binding),
                    Type = binding["Type"] as string,
                    DataType = DataType(binding),
                    Properties = { map }
                };
            }

            functionLoadRequests.Add(new RpcFunctionMetadata
            {
                FunctionId = sdkFunctionMetadata.Name,
                Name = sdkFunctionMetadata.Name,
                // Directory = sdkFunctionMetadata.FunctionDirectory,
                EntryPoint = sdkFunctionMetadata.EntryPoint,
                IsProxy = false,
                ScriptFile = sdkFunctionMetadata.ScriptFile,
                Bindings = { bindingInfos },
                ManagedDependencyEnabled = false
            });
        }

        await streamRequestStream.WriteAsync(new StreamingMessage
        {
            WorkerId = _options.Value.WorkerId,
            FunctionMetadataResponse = new FunctionMetadataResponse()
            {
                Result = new StatusResult() { Status = StatusResult.Types.Status.Success },
                FunctionMetadataResults = { functionLoadRequests }
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

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateMetadata(stoppingToken);
    }
}