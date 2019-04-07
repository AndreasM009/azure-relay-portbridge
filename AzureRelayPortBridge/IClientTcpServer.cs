using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AzureReleayPortBridge
{
    public interface IClientTcpServer
    {
        Task WriteAsync(Guid id, byte[] data);
    }
}
