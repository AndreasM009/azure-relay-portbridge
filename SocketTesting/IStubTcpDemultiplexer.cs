using System;
using System.Threading.Tasks;

namespace SocketTesting
{
    public interface IStubTcpDemultiplexer
    {
        Task Demultiplex(Guid hybridConnectionId, Guid id, int targetPort, byte[] data);
    }
}
