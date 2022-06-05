using System;
using System.Threading.Tasks;

namespace FunctionTestHost.TestHost;

public interface FunctionTestApp : IAsyncDisposable
{
    Task Start();
}