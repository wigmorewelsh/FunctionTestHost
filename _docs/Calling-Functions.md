
## Functions can be called via test host in the following ways:
1. via a http admin endpoint
2. via http calls including the function name
3. via extenstions to the sdk

### Calling via http admin endpoint
Every function app has an admin endpoint that can be used to call functions.
Given a testhost has already been set up, the admin endpoint can be called inside a test method as follows:

```csharp
var response = await _testCallbackHost.CallFunction("admin/SimpleServiceBusCall", Encoding.UTF8.GetBytes("bar"));
```

### Calling via http calls including the function name
When there is more than one function app under test, each function is exposed via a named function endpoint.
For example given a function app with two functions, `SampleFunctionOne` and `SampleFunctionTwo`, the functions can be called as follows:

```csharp
await _host.CallFunction("SampleFunctionOne/Hello");
await _host.CallFunction("SampleFunctionOne/Hello");
```

### Calling via extensions to the sdk
FunctionTestHost can be extended to provide other ways to call functions. Currently the only extension is for calling functions via the service bus.

TODO: add example



