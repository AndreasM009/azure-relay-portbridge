using System;
using System.Threading.Tasks;

namespace AzureReleayPortBridge
{
    public interface IServerTcpHybridConnectionServer
    {
        Task WriteAsync(Guid streamId, Guid id, byte[] data, int offset, int count);
    }
}
