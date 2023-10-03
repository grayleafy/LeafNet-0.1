using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LeafNet
{
    public class MsgHandler
    {
        public virtual void HandleMsg(IMessage baseMsg, NetEntity netEntity, Session session)
        {
            try
            {
                netEntity.logger.WriteLog("消息处理,来自:" + session.socket.RemoteEndPoint.ToString());
            }
            catch (SocketException e)
            {
                return;
            }
        }



        protected void BroadcastMessage<T>(T msg, NetEntity netEntity, Socket clientSocket) where T : IMessage
        {
            var s = typeof(T).Name;
        }

        protected void ReplyMessage<T>(T msg, NetEntity netEntity, Socket clientSocket) where T : IMessage
        {

        }
    }
}
