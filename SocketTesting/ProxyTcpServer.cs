using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class ProxyTcpServer : IProxyTcpServer
    {
        #region Fields

        private bool _run = true;
        private readonly int _port;
        private readonly int _remotePort;
        private readonly TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<Guid, TcpClient> _clients;
        private readonly object _syncRoot = new object();
        private readonly IProxyTcpMultiplexer _multiplexer;

        #endregion

        #region c'tor

        public ProxyTcpServer(int port, IProxyTcpMultiplexer multiplexer, int remotePort)
        {
            _clients = new Dictionary<Guid, TcpClient>();
            _cancellationTokenSource = new CancellationTokenSource();
            _port = port;
            _remotePort = remotePort;
            _tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
            _multiplexer = multiplexer;
        }

        #endregion

        #region Implementation

        async Task IProxyTcpServer.WriteAsync(Guid id, byte[] data)
        {
            var client = GetClient(id);

            if (null != client)
                await client.GetStream().WriteAsync(data);
        }

        public async Task Start()
        {
            _tcpListener.Start();

            await Task.Factory.StartNew(async () => 
            {
                while (_run)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    if (null == client || _cancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    client.NoDelay = true;
                    client.LingerState.Enabled = false;

                    var id = Guid.NewGuid();

                    lock (_syncRoot)
                    {
                        _clients.Add(id, client);
                    }

                    await OnNewClient(client, _cancellationTokenSource.Token, id);
                }
            });
        }

        public void Stop()
        {
            _tcpListener.Stop();
            _run = false;
            _cancellationTokenSource.Cancel();
        }

        #endregion

        #region Private

        private TcpClient GetClient(Guid id)
        {
            lock (_syncRoot)
            {
                TcpClient client = null;
                _clients.TryGetValue(id, out client);
                return client;
            }
        }

        private Task OnNewClient(TcpClient client, CancellationToken cancellationToken, Guid id)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    var buffer = new byte[65536];
                    var count = 0;
                    var memStream = new MemoryStream();
                        
                    while ((!cancellationToken.IsCancellationRequested) &&
                    (0 != (count = await client.GetStream().ReadAsync(buffer, 0, buffer.Length))))
                    {
                        _multiplexer.Mutliplex(id, _remotePort, buffer, 0, count);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                lock (_syncRoot)
                    _clients.Remove(id);
            });
        }

        #endregion
    }
}
