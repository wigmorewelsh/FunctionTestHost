using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppTwo;

public class HttpFunctionEndpoints
{
    [Function("Hello")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        string body = "";
        if (req.Body.Length != 0)
        {
            var reader = new StreamReader(req.Body);
            body = reader.ReadToEnd();
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Welcome to Azure Functions!");
        if(body != "")
            response.WriteString(body);

        return response;
    }
}