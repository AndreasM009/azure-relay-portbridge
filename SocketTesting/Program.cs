using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketTesting
{
    class Program
    {
        private const string ServiceBusDns = "anmockhybrid.servicebus.windows.net";

        public static void Main(string[] args)
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
            serviceProvider.GetService<HybridConnectionServerHost>().Run();
            serviceProvider.GetService<HybridConnectionClientHost>().Run();

            var clientTasks = new List<Task>();
            for (var i=0; i<10; i++)
            {
                int count = i;
                clientTasks.Add(Task.Factory.StartNew(async () => 
                {
                    var tc = new TestTcpClient("localhost", 4242, count);

                    for (var c = 0; c < 100; c++)
                    {
                        await tc.SendTestMessage($"{count} Hello World");
                        await tc.SendTestMessage($"{count} Hello World");
                    }
                }, TaskCreationOptions.LongRunning));
            }

            Task.WaitAll(clientTasks.ToArray());

            Console.In.ReadLine();
        }

        static async Task Send()
        {
            await Task.Factory.StartNew(async () =>
            {
                var client = new TcpClient("localhost", 4242);

                // FIRST WRITE
                var bytes = Encoding.ASCII.GetBytes("Hello!");
                var writeStream = new MemoryStream();
                
                // write message
                writeStream.Write(bytes, 0, bytes.Length);
                await writeStream.FlushAsync();
                // write socket
                await client.GetStream().WriteAsync(writeStream.ToArray(), 0, (int)writeStream.Position);
                await client.GetStream().FlushAsync();

                System.Threading.Thread.Sleep(30 * 1000);
                
                // SECOND WRITE
                bytes = Encoding.ASCII.GetBytes("World!");
                writeStream = new MemoryStream();
                // write message
                writeStream.Write(bytes, 0, bytes.Length);
                await writeStream.FlushAsync();
                // write socket
                await client.GetStream().WriteAsync(writeStream.ToArray(), 0, (int)writeStream.Position);
                await client.GetStream().FlushAsync();

                client.GetStream().Close();
                client.Close();
            });
        }
    }
}
