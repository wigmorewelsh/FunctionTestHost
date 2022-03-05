using Orleans.Core;

namespace FunctionTestHost.Actors;

public interface ILocalGrainCatalog
{
    FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity);
}