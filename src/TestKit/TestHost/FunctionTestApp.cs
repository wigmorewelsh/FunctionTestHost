using System;
using System.Threading.Tasks;

namespace TestKit.TestHost;

/// <summary>
/// A function app represents a azure function.
/// </summary>
public interface FunctionTestApp : IAsyncDisposable
{
    Task Start();
}