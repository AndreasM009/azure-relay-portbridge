using System;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var testServer = new TestTcpServer(4243);
            testServer.Start();

            Console.WriteLine("Server running, press ENTER key to exit");
            Console.In.ReadLine();
        }
    }
}
