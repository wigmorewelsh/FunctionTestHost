using Orleans;

namespace FunctionTestHost.Actors;

public enum FunctionState
{
    Init, FetchingMetadata, LoadingFunctions, Running
}