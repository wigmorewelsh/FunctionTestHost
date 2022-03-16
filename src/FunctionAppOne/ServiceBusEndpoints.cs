using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppOne;

public class ServiceBusEndpoints
{
    private readonly IThings _things;

    public ServiceBusEndpoints(IThings things)
    {
        _things = things;
    }

    [Function("SimpleServiceBusCall")]
    public void Run(
        [ServiceBusTrigger("somequeue")] byte[] data,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        _things.Called();
    }

}