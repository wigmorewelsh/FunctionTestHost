using Orleans.Core;

namespace TestKit.Actors;

internal interface ILocalGrainCatalog
{
    FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity);
}