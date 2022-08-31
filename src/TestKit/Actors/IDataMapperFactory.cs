using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans.Runtime;

namespace TestKit.Actors;

public interface IDataMapperFactory
{
    Task<DataMapper?> TryCreateDataMapper(FunctionLoadRequest loadRequest, IAddressable functionInstance);
}