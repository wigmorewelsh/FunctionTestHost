using Microsoft.Extensions.Hosting;

namespace FunctionAppTwo;

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