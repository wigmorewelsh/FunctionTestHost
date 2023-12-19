using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppOne;

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

    [Function("HelloTwo")]
    public HttpResponseData RunTwo(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData request,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Welcome to Azure Functions!");

        return response;
    }

    [Function("HelloTask")]
    public async Task<HttpResponseData> HelloTask(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData request,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Hello");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Welcome to Azure Functions!");

        return response;
    }

    [Function("ThrowTask")]
    public async Task<HttpResponseData> ThrowTask(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        throw new Exception("Error thrown");
    }
}