using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppOne;

public class ServiceBusEndpoints
{
    private readonly IExecutionCallback _executionCallback;

    public ServiceBusEndpoints(IExecutionCallback executionCallback)
    {
        _executionCallback = executionCallback;
    }

    [Function("SimpleServiceBusCall")]
    public void SimpleServiceBusCall(
        [ServiceBusTrigger("somequeue")] byte[] data,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        _executionCallback.Called();
    }

    [Function("BatchServiceBusCall")]
    public void BatchServiceBusCall(
        [ServiceBusTrigger("somequeue", IsBatched = true)] byte[][] data,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        _executionCallback.Called();
    }

}