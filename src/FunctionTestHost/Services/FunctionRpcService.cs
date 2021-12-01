using System;
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

        public FunctionRpcService(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
            IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            var functionGrain = await SetupFunctionGrain(requestStream);
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

            var response = await WaitTillInit(requestStream);

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

            await StartReading(requestStream);
        }

        private async Task StartReading(IAsyncStreamReader<StreamingMessage> requestStream)
        {
            while (await requestStream.MoveNext())
            {
                var dd = requestStream.Current;
                if (dd.InvocationResponse is { } response)
                {
                    var gg = response;
                    return;
                }
            }
        }

        private async Task<FunctionLoadResponse> WaitTillInit(IAsyncStreamReader<StreamingMessage> requestStream)
        {
            while (await requestStream.MoveNext())
            {
                var nextMessage = requestStream.Current;
                if (nextMessage.FunctionLoadResponse is { } initRequest)
                {
                    return initRequest;
                }
            }

            throw new Exception("Expected StartStream message");
        }

        private async Task<IFunctionGrain> SetupFunctionGrain(IAsyncStreamReader<StreamingMessage> requestStream)
        {
            if (await requestStream.MoveNext())
            {
                var nextMessage = requestStream.Current;
                if (nextMessage.StartStream is { } initRequest)
                {
                    return _grainFactory.GetGrain<IFunctionGrain>(initRequest.WorkerId);
                }
            }

            throw new Exception("Expected StartStream message");
        }
    }
}