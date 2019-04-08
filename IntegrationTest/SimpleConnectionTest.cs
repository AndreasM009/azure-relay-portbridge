using AzureReleayPortBridge;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TestServer;

namespace IntegrationTest
{
    [TestClass]
    public class SimpleConnectionTest
    {
        private static ServiceProvider _services;
        private static IConfiguration _configuration;
        private static TestTcpServer _server;

        [ClassInitialize]
        public static void TestSetup(TestContext context)
        {
            var configurationBuilder = new ConfigurationBuilder();
            var serviceCollection = new ServiceCollection();

            configurationBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false);

            var configuration = configurationBuilder.Build();

            serviceCollection
                .AddOptions()
                .AddLogging(c => c.AddConsole())
                .Configure<HybridConnectionServerOptions>(c => configuration.Bind("HybridConnectionServerHost", c))
                .Configure<HybridConnectionClientOptions>(c => configuration.Bind("HybridConnectionClientHost", c))
                .AddSingleton<HybridConnectionServerHost>()
                .AddSingleton<HybridConnectionClientHost>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<HybridConnectionServerHost>().Run().GetAwaiter().GetResult();
            serviceProvider.GetService<HybridConnectionClientHost>().Run().GetAwaiter().GetResult();

            _services = serviceProvider;
            _configuration = configuration;
            _server = new TestTcpServer(4243);
            _server.Start();
        }

        [ClassCleanup]
        public static void TestCleanup()
        {
            _services.GetService<HybridConnectionServerHost>().Stop().GetAwaiter().GetResult();
            _services.GetService<HybridConnectionClientHost>().Stop().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(_services);
            Assert.IsNotNull(_configuration);

            var client = new TestTcpClient("localhost", 4242);
            var result = client.SendTestMessage("Hello World!").GetAwaiter().GetResult();

            Assert.IsTrue(result == "Hello World!");
        }
    }
}
