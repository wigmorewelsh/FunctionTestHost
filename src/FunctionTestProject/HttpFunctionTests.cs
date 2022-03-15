using System;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FunctionAppOne;
using FunctionTestHost.TestHost;
using Shouldly;
using Xunit;

namespace FunctionTestProject;

public class HttpFunctionTests : IClassFixture<FunctionTestHost<Program>>
{
    private readonly FunctionTestHost<Program> _testHost;

    public HttpFunctionTests(FunctionTestHost<Program> testHost)
    {
        _testHost = testHost;
    }

    [Fact]
    public async Task CallSimpleFunction()
    {
        var response = await _testHost.CallFunction("Hello");
        response.ShouldBe("Welcome to Azure Functions!");
    }

    [Fact]
    public async Task CallSimpleFunction2()
    {
        var response = await _testHost.CallFunction("HelloTwo");
        response.ShouldBe("Welcome to Azure Functions!");
    }

    [Fact]
    public async Task CallTaskBasedFunction()
    {
        var response = await _testHost.CallFunction("HelloTask");
        response.ShouldBe("Welcome to Azure Functions!");
    }

    [Fact]
    public async Task CallFunctionWithJsonContent()
    {
        var body = JsonContent.Create("foo");
        var response = await _testHost.CallFunction("Hello", body);
        response.ShouldBe("Welcome to Azure Functions!\"foo\"");
    }

    [Fact]
    public async Task CallFunctionWithBytes()
    {
        var response = await _testHost.CallFunction("Hello", Encoding.UTF8.GetBytes("bar"));
        response.ShouldBe("Welcome to Azure Functions!bar");
    }
}