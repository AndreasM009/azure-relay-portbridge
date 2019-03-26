using Microsoft.Azure.Relay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class ProxyTcpHybridConnectionMultiplexer : IProxyTcpMultiplexer
    {
        #region Fields

        private readonly string _relayNamespace = "{RelayNamespace}.servicebus.windows.net";
        private readonly string _connectionName = "{HybridConnectionName}";
        private readonly string _keyName = "{SASKeyName}";
        private readonly string _key = "{SASKey}";
        private IProxyTcpServer _proxyTcpServer;
        private readonly object _syncRoot = new object();
        private readonly HybridConnectionClient _hybridConnectionClient;
        private HybridConnectionStream _hybridConnectionStream;

        #endregion

        #region c'tor

        public ProxyTcpHybridConnectionMultiplexer(
            string relayNamespace, 
            string connectionName,
            string keyName,
            string key)
        {
            _relayNamespace = relayNamespace;
            _connectionName = connectionName;
            _keyName = keyName;
            _key = key;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(_keyName, _key);

            _hybridConnectionClient = new HybridConnectionClient(
                new Uri(String.Format("sb://{0}/{1}", _relayNamespace, _connectionName)), tokenProvider);
        }

        #endregion

        #region Implementation

        public IProxyTcpServer ProxyTcpServer
        {
            set
            {
                _proxyTcpServer = value;
            }
        }

        public async Task Start()
        {
            var hybridConnectionStream = CreateConnection();

            await Task.Factory.StartNew(async () => 
            {                
                var buffer = new byte[65536];

                while (true)
                {
                    var id = Guid.Empty;    
                    var count = 0;
                    Int32 frameSize = 0;
                    Int32 bytesRead = 0;
                    var memStream = new MemoryStream();

                    count = await hybridConnectionStream.ReadAsync(buffer, 0, 16 + sizeof(Int32));
                    if (0 == count)
                        break;

                    id = new Guid(new ArraySegment<byte>(buffer, 0, 16).ToArray());
                    frameSize = BitConverter.ToInt32(new ArraySegment<byte>(buffer, 16, sizeof(Int32)).ToArray());

                    while (true)
                    {
                        var length = frameSize - bytesRead > buffer.Length ? buffer.Length : frameSize - bytesRead;
                        count = await hybridConnectionStream.ReadAsync(buffer, 0, length);

                        if (0 == count)
                            break;

                        bytesRead += count;
                        await memStream.WriteAsync(buffer, 0, count);

                        if (bytesRead == frameSize)
                        {
                            await _proxyTcpServer.WriteAsync(id, memStream.ToArray());
                            break;
                        }
                    }

                    if (0 == count)
                        break;
                }
            });
        }

        public void Stop()
        {
            if (null != _hybridConnectionStream)
                _hybridConnectionStream.Shutdown();
        }

        #endregion


        #region Implementation ITcpMultiplexer

        void IProxyTcpMultiplexer.Mutliplex(Guid tcpProxyId, int remotePort, byte[] data, int offset, int count)
        {
            CreateConnection();

            using (var memstream = new MemoryStream())
            {
                memstream.Write(tcpProxyId.ToByteArray());
                memstream.Write(BitConverter.GetBytes((Int32)remotePort));
                memstream.Write(BitConverter.GetBytes((Int32)count));
                memstream.Write(data, offset, count);

                lock (_syncRoot)
                {
                    _hybridConnectionStream.Write(memstream.ToArray());
                }
            }
        }

        #endregion

        #region Private implementation

        private HybridConnectionStream CreateConnection()
        {
            if (null == _hybridConnectionStream)
            {
                lock (_syncRoot)
                {
                    if (null == _hybridConnectionStream)
                    {
                        _hybridConnectionStream = _hybridConnectionClient.CreateConnectionAsync().Result;
                    }
                }
            }

            return _hybridConnectionStream;
        }

        #endregion
    }
}
