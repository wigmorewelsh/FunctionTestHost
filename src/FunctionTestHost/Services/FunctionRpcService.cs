using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using Grpc.Core;
using Orleans;

namespace FunctionTestHost
{
    public class FunctionRpcService : FunctionRpc.FunctionRpcBase
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ConnectionManager _connectionManager;

        public FunctionRpcService(IGrainFactory grainFactory, ConnectionManager connectionManager)
        {
            _grainFactory = grainFactory;
            _connectionManager = connectionManager;
        }

        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
            IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            var (workerId, functionGrain) = await SetupFunctionGrain(requestStream, context.CancellationToken);
            var channel = _connectionManager.Init(workerId);

            async Task Subscription()
            {
                await foreach (var message in channel.ReadAllAsync(context.CancellationToken))
                {
                    await responseStream.WriteAsync(message);
                }
            }

            var task = Subscription();
            
            await functionGrain.Init();
            
            await responseStream.WriteAsync(new StreamingMessage
            {
                FunctionLoadRequest = new FunctionLoadRequest
                {
                    FunctionId = "Hello",
                    Metadata = new RpcFunctionMetadata
                    {
                        IsProxy = false,
                        ScriptFile = "FunctionAppOne.dll",
                        Name = "Hello",
                        EntryPoint = "FunctionAppOne.Hello.Run",
                    }
                }
            });

            var response = await WaitTillInit(requestStream, context.CancellationToken);

            await responseStream.WriteAsync(new StreamingMessage
            {
                InvocationRequest = new InvocationRequest
                {
                    FunctionId = "Hello",
                    InvocationId = "123",
                    InputData =
                    {
                        new ParameterBinding
                        {
                            Name = "req",
                            Data = new TypedData
                            {
                                Http = new RpcHttp
                                {
                                }
                            }
                        }
                    },
                    TraceContext = new RpcTraceContext
                    {
                        TraceParent = "123"
                    }
                }
            });

            await StartReading(requestStream, context.CancellationToken);
            await task;
        }

        private async Task StartReading(IAsyncStreamReader<StreamingMessage> requestStream,
            CancellationToken contextCancellationToken)
        {
            while (await requestStream.MoveNext(contextCancellationToken))
            {
                var dd = requestStream.Current;
                if (dd.InvocationResponse is { } response)
                {
                    var gg = response;
                    return;
                }
            }
        }

        private async Task<FunctionLoadResponse> WaitTillInit(IAsyncStreamReader<StreamingMessage> requestStream,
            CancellationToken contextCancellationToken)
        {
            while (await requestStream.MoveNext(contextCancellationToken))
            {
                var nextMessage = requestStream.Current;
                if (nextMessage.FunctionLoadResponse is { } initRequest)
                {
                    return initRequest;
                }
            }

            throw new Exception("Expected StartStream message");
        }

        private async Task<(string, IFunctionGrain)> SetupFunctionGrain(
            IAsyncStreamReader<StreamingMessage> requestStream, CancellationToken contextCancellationToken)
        {
            if (await requestStream.MoveNext(contextCancellationToken))
            {
                var nextMessage = requestStream.Current;
                if (nextMessage.StartStream is { } initRequest)
                {
                    return (initRequest.WorkerId, _grainFactory.GetGrain<IFunctionGrain>(initRequest.WorkerId));
                }
            }

            throw new Exception("Expected StartStream message");
        }
    }
}