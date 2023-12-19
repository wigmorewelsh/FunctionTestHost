using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;

namespace TestKit.Actors;

public class FunctionRegistoryGrain : Grain, IFunctionRegistoryGrain 
{
    private enum Status
    {
        Loading, Loaded
    }
    
    private Dictionary<string, Status> _functions = new();
    private HashSet<IStatusSubscriber> _observers = new();
    
    public Task RegisterFunction(string functionId)
    {
        _functions[functionId] = Status.Loading;
        return Task.CompletedTask;
    }
    
    public async Task UpdateFunction(string functionId)
    {
        _functions[functionId] = Status.Loaded;
        await TryNotifyObservers();
    }

    private async Task TryNotifyObservers()
    {
        if (_functions.Any() && _functions.All(x => x.Value == Status.Loaded))
        {
            foreach (var observer in _observers)
            {
                await observer.Notify();
            }
        }
    }

    public Task AddObserver(IStatusSubscriber observer)
    {
        _observers.Add(observer);
        return Task.CompletedTask;
    }
}

