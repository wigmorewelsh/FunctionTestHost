using System;

namespace TestKit.Utils;

public interface ITestClusterPortAllocator : IDisposable
{
    ValueTuple<int, int> AllocateConsecutivePortPairs(int numPorts);
}