using System;
using System.Threading.Tasks;

namespace TestKit.TestHost;

public interface FunctionTestApp : IAsyncDisposable
{
    Task Start();
}