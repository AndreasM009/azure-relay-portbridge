using System;
using System.Threading.Tasks;

namespace SocketTesting
{
    public interface IServerTcpDemultiplexer
    {
        Task Demultiplex(Guid hybridConnectionId, Guid id, int targetPort, byte[] data);
        Task ClientConnectionClosed(Guid hybridConnectionId, Guid id);
    }
}
