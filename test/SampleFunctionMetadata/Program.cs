using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;

namespace FunctionAppOne;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        host.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return new HostBuilder()
            .ConfigureFunctionsWorkerDefaults();
    }
}