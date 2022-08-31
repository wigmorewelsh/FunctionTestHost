using System;
using Microsoft.Extensions.Hosting;

namespace TestKit.TestHost;

public interface ITestHostBuilder
{
    ITestHostBuilder WithServiceConfiguration(Action<IHostBuilder> action);
    
}