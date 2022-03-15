using System;

namespace FunctionTestHost.Utils;

public interface ITestClusterPortAllocator : IDisposable
{
    ValueTuple<int, int> AllocateConsecutivePortPairs(int numPorts);
}