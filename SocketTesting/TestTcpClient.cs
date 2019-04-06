using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketTesting
{
    public class TestTcpClient
    {
        private readonly TcpClient _client;
        private readonly int _id;

        public TestTcpClient(string hostname, int port, int id)
        {
            _id = id;
            _client = new TcpClient(hostname, port);
        }

        public async Task SendTestMessage(string message)
        {
            var streamWriter = new StreamWriter(_client.GetStream());
            streamWriter.AutoFlush = true;
            await streamWriter.WriteLineAsync(message);

            var streamReader = new StreamReader(_client.GetStream());
            var echo = await streamReader.ReadLineAsync();
            Console.WriteLine($"{_id} Echo from server: {echo}");
        }

        public void Close()
        {
            _client.Close();
        }
    }
}
