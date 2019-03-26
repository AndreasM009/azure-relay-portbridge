using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class StubTcpHybridConnectionDemultiplexer : IStubTcpDemultiplexer
    {
        #region Fields

        private readonly string _forwardHostName;
        private Dictionary<Guid, TcpClient> _forwardClients = new Dictionary<Guid, TcpClient>();
        private readonly object _syncRoot = new object();
        private readonly IStubTcpHybridConnectionServer _hybridConnectionServer;

        #endregion

        #region c'tor 

        public StubTcpHybridConnectionDemultiplexer(string forwardHostName, IStubTcpHybridConnectionServer server)
        {            
            _forwardHostName = forwardHostName;
            _hybridConnectionServer = server;
        }

        #endregion        

        #region Private Implementation

        private Task OnNewForwardClient(Guid streamId, TcpClient tcpClient, Guid id)
        {
            return Task.Factory.StartNew(async () => 
            {
                var buffer = new byte[65536];
                var count = 0;

                while (0 != (count = await tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length)))
                {
                    await _hybridConnectionServer.WriteAsync(streamId, id, buffer, 0, count);
                }

                lock (_syncRoot)
                {
                    _forwardClients.Remove(id);
                }
            });
        }

        #endregion

        #region IStubTcpDemultiplexer implemenation

        async Task IStubTcpDemultiplexer.Demultiplex(Guid hybridConnectionId, Guid id, int targetPort, byte[] data)
        {
            TcpClient client = null;
            TcpClient tmp = null;
            var isNew = false;

            lock (_syncRoot)
            {
                _forwardClients.TryGetValue(id, out client);
            }

            if (null == client)
            {
                client = new TcpClient(_forwardHostName, targetPort);
                client.LingerState.Enabled = true;
                client.NoDelay = true;

                lock (_syncRoot)
                {
                    
                    if (!_forwardClients.TryGetValue(id, out tmp))
                    {
                        isNew = true;
                        _forwardClients.Add(id, client);
                    }
                }

                if (isNew)
                {
                    await OnNewForwardClient(hybridConnectionId, client, id);
                }
                else
                {
                    client.Close();
                    client = tmp;
                }
            }

            await client.GetStream().WriteAsync(data);
        }

        #endregion
    }
}
