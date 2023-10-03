using Google.Protobuf;
using LeafNetCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeafNet
{
    public class NetEntity
    {
        public Logger logger;

        protected ClassMapper<MessageParser> msgParserMapper = new ClassMapper<MessageParser>("", ""); //根据名称获取消息反序列化泛型类的缓存
        protected ClassMapper<MsgHandler> msgHandlerMapper = new ClassMapper<MsgHandler>("Handler", "LeafNet."); //根据名称获取消息处理类


        protected Queue<(string, IMessage, Session)> reciveMsgs = new Queue<(string, IMessage, Session)>(); //收到后待处理的消息
        protected Queue<(string, IMessage, Session)> sendMsgs = new Queue<(string, IMessage, Session)>(); //待发送的消息
        protected Thread reciveThread = null; //消息接收线程
        protected Thread dispatchThread = null; //消息分发处理线程
        protected Thread sendThread = null; //消息发送线程

        //关闭会话
        public virtual void Close(Session session)
        {
            //TODO 主动发送断开请求
            //消息分发
            //MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
            //object[] ob = { state };
            //mei.Invoke(null, ob);
            //关闭
            session.socket.Close();
            
        }

        //会话接收到消息
        protected void ReciveSession(Session session)
        {
            logger.WriteLog("接收到消息,来自" + session.socket.RemoteEndPoint.ToString());
            var clientfd = session.socket;
            Session state = session;
            

            ByteArray readBuff = state.readBuff;
            //接收
            int count = 0;
            //缓冲区不够，清除，若依旧不够，只能返回
            //当单条协议超过缓冲区长度时会发生
            if (readBuff.remain <= 0)
            {
                DecodeData(state);
                readBuff.MoveBytes();
            };
            if (readBuff.remain <= 0)
            {
                Console.WriteLine("Receive fail , maybe msg length > buff capacity");
                Close(state);
                return;
            }
            try
            {
                count = clientfd.Receive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Receive SocketException " + ex.ToString());
                Close(state);
                return;
            }
            //客户端关闭
            if (count <= 0)
            {
                Console.WriteLine("Socket Close " + clientfd.RemoteEndPoint.ToString());
                Close(state);
                return;
            }
            //消息处理
            readBuff.writeIdx += count;
            //处理二进制消息
            DecodeData(state);
            //移动缓冲区
            readBuff.CheckAndMoveBytes();
        }

        //反序列化二进制消息，加入待分发队列
        void DecodeData(Session session)
        {
            if (session.readBuff.DecodeMsg(out string msgName, out IMessage msg, msgParserMapper))
            {
                lock (reciveMsgs)
                {
                    reciveMsgs.Enqueue((msgName, msg, session));
                    Monitor.Pulse(reciveMsgs);
                }
            }
        }

       

        

        //消息发送循环
        protected void SendLoop()
        {
            while (true)
            {
                lock (sendMsgs)
                {
                    while (sendMsgs.Count == 0)
                    {
                        Monitor.Wait(sendMsgs);
                    }
                }
                ReallySendAllMsg();
            }
        }

        /// <summary>
        /// 处理一条队列中的消息
        /// </summary>
        public void DisPatchMsg()
        {
            (string, IMessage, Session) msgInfo = default;
            lock (reciveMsgs)
            {
                if (reciveMsgs.Count == 0)
                {
                    return;
                }

                msgInfo = reciveMsgs.Dequeue();
            }

            MsgHandler msgHandler = msgHandlerMapper.GetClass(msgInfo.Item1);
            msgHandler.HandleMsg(msgInfo.Item2, this, msgInfo.Item3);
        }

        /// <summary>
        /// 发送队列中待发的所有消息
        /// </summary>
        protected void ReallySendAllMsg()
        {
            List<Session> sessions = new List<Session>();

            //把消息写入会话缓冲区
            lock (sendMsgs)
            {
                if (sendMsgs.Count == 0) { return; }

                while (sendMsgs.Count > 0) {
                    var msgInfo = sendMsgs.Dequeue();
                    msgInfo.Item3.writeBuff.EncodeMsg(msgInfo.Item1, msgInfo.Item2);
                    sessions.Add(msgInfo.Item3);
                }
            }

            //发送二进制数据
            foreach (Session session in sessions)
            {
                try
                {
                    if (session == null)
                    {
                        continue;
                    }
                    if (session.socket == null)
                    {
                        continue;
                    }
                    if (session.socket.Connected == false)
                    {
                        continue;
                    }


                    byte[] sendBytes = new byte[session.writeBuff.length];
                    session.writeBuff.Read(sendBytes, 0, sendBytes.Length);
                    session.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
                }
                catch (SocketException ex) 
                {
                    logger.WriteLog("send fail " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <param name="session"></param>
        public void SendMessage<T>(T msg, Session session)
        {
            lock (sendMsgs)
            {
                string name = typeof(T).Name;
                sendMsgs.Enqueue((name, msg as IMessage, session));
                Monitor.Pulse(sendMsgs);
            }
        }
    }
}
