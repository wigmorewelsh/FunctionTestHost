using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans.Runtime;

namespace TestKit.Actors;

public interface IDataMapperFactory
{
    Task<DataMapper?> TryCreateDataMapper(RpcFunctionMetadata loadRequest, IAddressable functionInstance);
}