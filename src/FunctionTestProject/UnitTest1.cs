using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace FunctionTestProject
{
    public class UnitTest1 : IClassFixture<FunctionTestHost>
    {
        private readonly FunctionTestHost _testHost;

        public UnitTest1(FunctionTestHost testHost)
        {
            _testHost = testHost;
        }

        [Fact]
        public async Task Test1()
        {
            await _testHost.CreateServer();
        }
    }
}