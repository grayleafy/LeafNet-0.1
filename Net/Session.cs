using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LeafNet
{
    public class Session
    {
        public Socket socket;
        //Ping
        public long lastPingTime = 0;
        //消息缓冲区
        public ByteArray readBuff = new ByteArray();
        public ByteArray writeBuff = new ByteArray();
    }
}
