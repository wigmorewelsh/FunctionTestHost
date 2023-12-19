using System.Threading.Tasks;
using Orleans;

namespace TestKit.Actors;

public interface IStatusSubscriber : IGrainObserver
{
    Task Notify();
}