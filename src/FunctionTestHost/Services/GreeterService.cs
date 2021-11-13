using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace FunctionTestHost
{
    public class FunServ : FunctionRpc.FunctionRpcBase
    {
        public override Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            return base.EventStream(requestStream, responseStream, context);
        }
    }
    
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}