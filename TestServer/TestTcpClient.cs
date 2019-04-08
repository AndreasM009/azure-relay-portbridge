using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestServer
{
    public class TestTcpClient
    {
        private readonly TcpClient _client;

        public TestTcpClient(string hostname, int port)
        {
            _client = new TcpClient(hostname, port);
        }

        public async Task<string> SendTestMessage(string message)
        {
            var streamWriter = new StreamWriter(_client.GetStream());
            streamWriter.AutoFlush = true;
            await streamWriter.WriteLineAsync(message);

            var streamReader = new StreamReader(_client.GetStream());
            var echo = await streamReader.ReadLineAsync();
            return echo;
        }

        public void Close()
        {
            _client.Close();
        }
    }
}
