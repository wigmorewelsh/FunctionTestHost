using Orleans.Core;

namespace TestKit.Actors;

public interface ILocalGrainCatalog
{
    FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity);
}