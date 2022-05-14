using System;
using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FunctionAppOne;

public class CheckSetup
{
    private readonly IConfiguration _configuration;

    public CheckSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("CheckSettings")]
    public HttpResponseData RCheckSettingsun(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")]
        HttpRequestData req,
        FunctionContext executionContext)
    {

        var settingValue = _configuration.GetValue<string>("SomeSetting");
        if (settingValue != "SomeValue")
            throw new Exception("Settings not correct");


        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Welcome to Azure Functions!");

        return response;
    }

}