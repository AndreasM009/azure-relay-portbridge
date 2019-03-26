using System;
using System.Threading.Tasks;

namespace SocketTesting
{
    public interface IStubTcpHybridConnectionServer
    {
        Task WriteAsync(Guid streamId, Guid id, byte[] data, int offset, int count);
    }
}
