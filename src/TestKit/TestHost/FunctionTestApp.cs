using System;
using System.Threading.Tasks;
using Orleans;

namespace TestKit.TestHost;

public interface FunctionTestApp : IAsyncDisposable
{
    Task Start(IGrainFactory grainFactory);
}