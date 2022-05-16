using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FunctionAppOne;
using FunctionTestHost.TestHost;
using Shouldly;
using Xunit;

namespace FunctionTestProject;

public class SetupFunctionTests : IClassFixture<FunctionTestHost<Program>>
{
    private readonly FunctionTestHost<Program> _testHost;

    public SetupFunctionTests(FunctionTestHost<Program> testHost)
    {
        _testHost = testHost;
    }

    [Fact]
    public async Task CallSimpleFunction()
    {
        var response = await _testHost.CallFunction("CheckSettings");
        response.ShouldBe("Welcome to Azure Functions!");
    }


}