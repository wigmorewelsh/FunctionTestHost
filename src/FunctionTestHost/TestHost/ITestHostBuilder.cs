using System;
using Microsoft.Extensions.Hosting;

namespace FunctionTestHost.TestHost;

public interface ITestHostBuilder
{
    ITestHostBuilder WithServiceConfiguration(Action<IHostBuilder> action);
    
}