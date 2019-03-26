using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestServer
{
    public class TestTcpServer
    {
        private readonly TcpListener _tcpListener;

        public TestTcpServer(int port)
        {
            _tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        }

        public Task Start()
        {
            _tcpListener.Start();

            return Task.Factory.StartNew(async () => 
            {
                while (true)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    if (null == client)
                        return;

                    await OnNewClient(client);
                }
            });
        }

        private Task OnNewClient(TcpClient client)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var reader = new StreamReader(client.GetStream());
                    var msg = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(msg))
                        break;
                    
                    var writer = new StreamWriter(client.GetStream());
                    writer.AutoFlush = true;
                    await writer.WriteLineAsync(msg);
                }
            });
        }
    }
}
