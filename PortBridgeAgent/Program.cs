using AzureReleayPortBridge;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace PortBridgeAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            var serviceCollection = new ServiceCollection();

            configurationBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            serviceCollection
                .AddOptions()
                .AddLogging(c => c.AddConsole())
                .Configure<HybridConnectionClientOptions>(c => configuration.Bind("PortBridgeAgent", c))
                .AddSingleton<HybridConnectionClientHost>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<HybridConnectionClientHost>().Run().GetAwaiter().GetResult();

            while (true)
                System.Threading.Thread.Sleep(1000);
        }
    }
}
