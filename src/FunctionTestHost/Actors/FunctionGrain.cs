using System.Threading.Tasks;
using Orleans;
using Orleans.Placement;

namespace FunctionTestHost.Actors
{
    public interface IFunctionGrain : IGrainWithStringKey
    {
        Task Init();
    }

    public enum FunctionState
    {
        Init, FetchingMetadata, LoadingFunctions, Running
    }
    
    [PreferLocalPlacement]
    public class FunctionGrain : Grain, IFunctionGrain
    {
        private FunctionState State = FunctionState.Init;
        public Task Init()
        {
            State = FunctionState.Init;
            return Task.CompletedTask;
        }
    }
}