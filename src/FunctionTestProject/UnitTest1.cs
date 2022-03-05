using System;
using System.Threading.Tasks;
using FunctionAppOne;
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
        await _testHost.CallFunction("123");
    }
}