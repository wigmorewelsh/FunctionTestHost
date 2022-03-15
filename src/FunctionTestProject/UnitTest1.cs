using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FunctionAppOne;
using Shouldly;
using Xunit;

namespace FunctionTestProject;

public class UnitTest1 : IClassFixture<FunctionTestHost<Program>>
{
    private readonly FunctionTestHost<Program> _testHost;

    public UnitTest1(FunctionTestHost<Program> testHost)
    {
        _testHost = testHost;
    }

    [Fact]
    public async Task Test1()
    {
        var response = await _testHost.CallFunction("Hello");
        response.ShouldBe("Welcome to Azure Functions!");
    }


    [Fact]
    public async Task Test2()
    {
        var response = await _testHost.CallFunction("HelloTwo");
        response.ShouldBe("Welcome to Azure Functions!");
    }

    [Fact]
    public async Task TestTask()
    {
        var response = await _testHost.CallFunction("HelloTask");
        response.ShouldBe("Welcome to Azure Functions!");
    }


    [Fact(Skip = "next")]
    public async Task Test1WithBody()
    {
        var body = JsonContent.Create("foo");
        var response = await _testHost.CallFunction("Hello", body);
        response.ShouldBe("Welcome to Azure Functions!");
    }
}