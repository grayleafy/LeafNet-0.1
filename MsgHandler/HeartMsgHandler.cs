using Google.Protobuf;
using LeafNetCore.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LeafNet
{
    public class HeartMsgHandler : MsgHandler
    {
        public override void HandleMsg(IMessage baseMsg, NetEntity netEntity, Session session)
        {
            base.HandleMsg(baseMsg, netEntity, session);
            session.lastPingTime =NetTool.GetTimeStamp();
            netEntity.logger.WriteLog("心跳消息");
        }
    }
}
