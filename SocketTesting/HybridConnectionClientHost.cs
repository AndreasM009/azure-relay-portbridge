using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class HybridConnectionClientHost
    {
        private readonly HybridConnectionClientOptions _options;
        private readonly ILogger<HybridConnectionClientHost> _logger;

        public HybridConnectionClientHost(
            IOptions<HybridConnectionClientOptions> options,
            ILogger<HybridConnectionClientHost> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task Run()
        {
            _logger.LogInformation($"Starting Hybrid Connection clients...");

            foreach (var config in _options.ForwardingRules)
            {
                var multiplexer = new ClientTcpHybridConnectionMultiplexer(
                    _options.ServiceBusNamespace, 
                    config.ServiceBusConnectionName, 
                    _options.ServiceBusKeyname, 
                    _options.ServiceBuskey,
                    _logger);

                var server = new ClientTcpServer(config.LocalPort, multiplexer, config.RemotePort, _logger);
                multiplexer.ProxyTcpServer = server;

                _logger.LogInformation($"Starting Tcp Server on local port {config.LocalPort} and mapping to remote port {config.RemotePort} using Hybrid Connection {_options.ServiceBusNamespace}/{config.ServiceBusConnectionName}.");

                multiplexer.Start().GetAwaiter().GetResult();
                server.Start().GetAwaiter().GetResult();
            }

            return Task.Delay(0);
        }
    }
}
