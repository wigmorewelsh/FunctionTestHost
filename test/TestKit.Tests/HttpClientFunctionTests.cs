using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FunctionAppOne;
using Shouldly;
using TestKit.TestHost;
using Xunit;

namespace TestKit.Tests;

public class HttpClientFunctionTests : IClassFixture<FunctionTestHost<Program>>
{
    private readonly FunctionTestHost<Program> _testHost;

    public HttpClientFunctionTests(FunctionTestHost<Program> testHost)
    {
        _testHost = testHost;
    }

    [Fact]
    public async Task CallSimpleFunction()
    {
        var client = _testHost.CreateClient();
        var response = await client.GetAsync("/Hello");
        var result = await response.Content.ReadAsStringAsync();
        result.ShouldBe("Welcome to Azure Functions!");
    }

    // [Fact]
    // public async Task CallSimpleFunction2()
    // {
    //     var response = await _testHost.CallFunction("HelloTwo");
    //     response.ShouldBe("Welcome to Azure Functions!");
    // }
    //
    // [Fact]
    // public async Task CallTaskBasedFunction()
    // {
    //     var response = await _testHost.CallFunction("HelloTask");
    //     response.ShouldBe("Welcome to Azure Functions!");
    // }
    //
    // [Fact]
    // public async Task CallFunctionWithJsonContent()
    // {
    //     var body = JsonContent.Create("foo");
    //     var response = await _testHost.CallFunction("Hello", body);
    //     response.ShouldBe("Welcome to Azure Functions!\"foo\"");
    // }
    //
    // [Fact]
    // public async Task CallFunctionWithBytes()
    // {
    //     var response = await _testHost.CallFunction("Hello", Encoding.UTF8.GetBytes("bar"));
    //     response.ShouldBe("Welcome to Azure Functions!bar");
    // }
    //
    // [Fact]
    // public async Task CallFunction_Throws()
    // {
    //     var response = await _testHost.CallFunction("ThrowTask");
    //     response.ShouldContain("Error thrown");
    // }
    //
    // [Fact]
    // public async Task CallFunctionWithBytes_Throws()
    // {
    //     var response = await _testHost.CallFunction("ThrowTask", Encoding.UTF8.GetBytes("bar"));
    //     response.ShouldContain("Error thrown");
    // }
}