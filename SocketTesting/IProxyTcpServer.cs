using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketTesting
{
    public interface IProxyTcpServer
    {
        Task WriteAsync(Guid id, byte[] data);
    }
}
