using Google.Protobuf;
using LeafNetCore.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeafNet
{
    public class ServerEntity : NetEntity
    {
        public int clientsLimit = 1024; //最大客户端数量
        public long heartCheckTime = 1000; //每秒一次心跳检测，断开客户端连接


        EndPoint listenAddress = null;  //监听地址

        Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //监听socket
        Dictionary<Socket, Session> clients = new Dictionary<Socket, Session>(); 
        List<Socket> checkRead = new List<Socket>(); //select检测列表

        public ServerEntity(EndPoint listenAddress, Logger logger)
        {
            this.listenAddress = listenAddress;
            this.logger = logger;
        }

        public void Start()
        {
            listenfd.Bind(listenAddress);
            listenfd.Listen(clientsLimit);
            reciveThread = new Thread(ReciveLoop);
            dispatchThread = new Thread(ServerDisPatchLoop);
            sendThread = new Thread(SendLoop);
            reciveThread.Start();
            dispatchThread.Start();
            sendThread.Start();
        }

        //接收连接请求和消息
        void ReciveLoop()
        {
            while (true)
            {
                ResetCheckSockets();
                Socket.Select(checkRead, null, null, 1000);
                for (int i = checkRead.Count - 1; i >= 0; --i)
                {
                    if (checkRead[i] == listenfd)
                    {
                        ReadListenfd(listenfd);
                    }
                    else
                    {
                        ReadClientfd(checkRead[i]);
                    }
                }
            }
        }

        private void ReadClientfd(Socket socket)
        {
            Session session = null;
            lock (clients)
            {
                session = clients[socket];
            }

            if (session == null)
            {
                return;
            }

            ReciveSession(session);
        }

        //读取Listen
        public void ReadListenfd(Socket listenfd)
        {
            try
            {
                Socket clientfd = listenfd.Accept();
                Console.WriteLine("Accept " + clientfd.RemoteEndPoint.ToString());
                Session session = new Session();
                session.socket = clientfd;
                session.lastPingTime = NetTool.GetTimeStamp();
                lock (clients)
                {
                    clients.Add(clientfd, session);
                }      
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Accept fail" + ex.ToString());
            }
        }

        

        public override void Close(Session session)
        {
            base.Close(session);
            lock (clients)
            {
                clients.Remove(session.socket);
            } 
        }
        

        //重置select列表
        void ResetCheckSockets()
        {
            checkRead.Clear();
            checkRead.Add(listenfd);
            lock (clients)
            {
                foreach (var socket in clients.Keys)
                {
                    checkRead.Add(socket);
                }
            }
        }

        //消息分发处理循环
        void ServerDisPatchLoop()
        {
            long lastHeartCheckTime = NetTool.GetTimeStamp();
            while (true)
            {
                lock (reciveMsgs)
                {
                    while (reciveMsgs.Count == 0)
                    {
                        Monitor.Wait(reciveMsgs);
                    }
                }

                DisPatchMsg();

                //根据心跳更新连接状态
                long currentTime = NetTool.GetTimeStamp();
                if (currentTime - lastHeartCheckTime >= heartCheckTime)
                {
                    lastHeartCheckTime = currentTime;
                    HeartCheck();
                }
            }
        }

        //心跳检测
        void HeartCheck()
        {
            logger.WriteLog("心跳检测");
            lock (clients)
            {
                long currentTime = NetTool.GetTimeStamp();
                var sockets = clients.Keys;
                foreach (var socket in sockets)
                {
                    var session = clients[socket];
                    if (currentTime - session.lastPingTime > heartCheckTime)
                    {
                        logger.WriteLog("移除连接" + socket.RemoteEndPoint.ToString());
                        Close(session);
                        clients.Remove(socket);
                    }
                }
            }
        }
    }
}
