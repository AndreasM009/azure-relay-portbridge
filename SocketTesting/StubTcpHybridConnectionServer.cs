using Microsoft.Azure.Relay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class StubTcpHybridConnectionServer : IStubTcpHybridConnectionServer
    {
        #region Fields

        private readonly string _relayNamespace = "{RelayNamespace}.servicebus.windows.net";
        private readonly string _connectionName = "{HybridConnectionName}";
        private readonly string _keyName = "{SASKeyName}";
        private readonly string _key = "{SASKey}";
        private readonly HashSet<int> _validPorts;
        private readonly HybridConnectionListener _hybridConnectionListener;
        private readonly object _syncRoot = new object();
        private readonly CancellationTokenSource _cts;
        private IStubTcpDemultiplexer _demultiplexer;
        private readonly Dictionary<Guid, HybridConnectionStream> _hybridConnectionStreams;

        #endregion

        #region c'tor

        public StubTcpHybridConnectionServer(
            string relayNamespace,
            string connectionName,
            string keyName,
            string key,
            HashSet<int> validPorts)
        {
            _relayNamespace = relayNamespace;
            _connectionName = connectionName;
            _keyName = keyName;
            _key = key;
            _validPorts = validPorts;
            _hybridConnectionStreams = new Dictionary<Guid, HybridConnectionStream>();
            _cts = new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(_keyName, _key);
            _hybridConnectionListener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", _relayNamespace, _connectionName)), tokenProvider);
        }

        #endregion

        #region Implementation

        public IStubTcpDemultiplexer Demultiplexer
        {
            set { _demultiplexer = value; }
        }

        public async Task Start()
        {
            await _hybridConnectionListener.OpenAsync(_cts.Token);

            await Task.Factory.StartNew(async () =>
            {
                _cts.Token.Register(() => _hybridConnectionListener.CloseAsync(CancellationToken.None));

                while (true)
                {
                    var client = await _hybridConnectionListener.AcceptConnectionAsync();
                    if (null == client)
                    {
                        await _hybridConnectionListener.CloseAsync(CancellationToken.None);
                        return;
                    }

                    var streamId = Guid.NewGuid();

                    lock (_syncRoot)
                    {
                        _hybridConnectionStreams.Add(streamId, client);
                    }

                    await OnNewClient(streamId, client, _cts.Token);
                }
            });
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        #endregion

        #region Private implementation

        private Task OnNewClient(Guid streamId, HybridConnectionStream stream, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                var buffer = new byte[65536];

                while (true)
                {
                    var id = Guid.Empty;
                    int remotePort = 0;
                    var count = 0;
                    Int32 frameSize = 0;
                    Int32 bytesRead = 0;
                    var memStream = new MemoryStream();

                    count = await stream.ReadAsync(buffer, 0, 16 + sizeof(Int32) + sizeof(Int32));

                    if (0 == count)
                        break;

                    id = new Guid(new ArraySegment<byte>(buffer, 0, 16).ToArray());
                    remotePort = BitConverter.ToInt32(new ArraySegment<byte>(buffer, 16, sizeof(Int32)).ToArray());
                    frameSize = BitConverter.ToInt32(new ArraySegment<byte>(buffer, 16 + sizeof(Int32), sizeof(Int32)).ToArray());

                    if (!_validPorts.Contains(remotePort))
                    {
                        Console.WriteLine($"Connection on port {remotePort} not allowed for hybrid connectio  {_connectionName}.");

                        stream.Close();
                    }

                    while (true)
                    {
                        var length = frameSize - bytesRead > buffer.Length ? buffer.Length : frameSize - bytesRead;
                        count = await stream.ReadAsync(buffer, 0, length);

                        if (0 == count || token.IsCancellationRequested)
                            break;

                        bytesRead += count;
                        await memStream.WriteAsync(buffer, 0, count);

                        if (bytesRead == frameSize)
                        {
                            await _demultiplexer.Demultiplex(streamId, id, remotePort, memStream.ToArray());
                            break;
                        }
                    }

                    if (0 == count || token.IsCancellationRequested)
                        break;
                }

                lock (_syncRoot)
                {
                    _hybridConnectionStreams.Remove(streamId);
                }

                await stream.ShutdownAsync(_cts.Token);
            });
        }

        Task IStubTcpHybridConnectionServer.WriteAsync(Guid streamId, Guid id, byte[] data, int offset, int count)
        {
            lock (_syncRoot)
            {
                HybridConnectionStream stream = null;
                if (!_hybridConnectionStreams.TryGetValue(streamId, out stream))
                {
                    Console.WriteLine($"Hybrid connection stream not available for connection {_connectionName}");
                    return Task.Delay(0);
                }

                var memstream = new MemoryStream();
                memstream.Write(id.ToByteArray());
                memstream.Write(BitConverter.GetBytes((Int32)count));
                memstream.Write(data, offset, count);

                stream.Write(memstream.ToArray());
                return Task.Delay(0);
            }
        }

        #endregion
    }
}
