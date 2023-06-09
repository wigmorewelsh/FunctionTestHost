
## Changes needed to the Azure Function project to support testing with the TestHost.

Similar to the aspnet TestHost this project expects a static method to be exposed in order to configure the appliction for testing.  The method must be named `CreateHostBuilder` and accept a `string[]` as an argument.  The method must return an `IHostBuilder` instance.  The `IHostBuilder` instance will be used to create the `IHost` instance that will be used to host the function.
 
For example:
```csharp
private static IHostBuilder CreateHostBuilder(string[] args)
{
    return new HostBuilder()
        .ConfigureFunctionsWorkerDefaults();
}
```

### Testing using xunit

To use start and use the function app in a test you can use the `FunctionTestHost<T>` class. The `FunctionTestHost<T>` class takes care of starting up the function app, initializing any functions you've written and acts as a mediator between your tests and the function app.

```csharp
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
}
```

### Customizing the test host

The function under test can be customized by overriding the `ConfigureFunction` method on the `FunctionTestHost<T>` class.  This method is called before the function app is started and can be used to add services to the function app's service collection.

```csharp
public class FunctionTestCallbackHost : FunctionTestHost<Program>
{
    public override void ConfigureFunction(IHostBuilder host)
    {
        host.ConfigureServices(services =>
        {
           ... add services here ...
        });
    }
}
```


