﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SocketTesting
{
    public interface IProxyTcpMultiplexer
    {
        void Mutliplex(Guid tcpProxyId, int remotePort, byte[] data, int offset, int count);
    }
}
