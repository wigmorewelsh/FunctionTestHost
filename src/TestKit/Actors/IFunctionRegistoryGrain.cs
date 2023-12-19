using System.Threading.Tasks;
using Orleans;

namespace TestKit.Actors;

public interface IFunctionRegistoryGrain : IGrainWithIntegerKey
{
    Task RegisterFunction(string functionId);
    Task UpdateFunction(string functionId);
    Task AddObserver(IStatusSubscriber observer);
}