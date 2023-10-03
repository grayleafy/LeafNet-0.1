using LeafNet;
using LeafNetCore.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeafNetCore
{
    public class ClientEntity : NetEntity
    {
        public int disPatchCount = 50; //每帧处理的消息条数

        EndPoint serverEndPoint;
        Socket clientSocket;
        Session serverSession = new Session();

        //心跳
        long lastHeartTime = 0;
        long heartDeltaTime = 500;

        //状态 
        public bool connected = false; //是否已连接状态
        bool isConnecting = false; //是否正在连接
        

        public ClientEntity(EndPoint serverEndPoint, Logger logger)
        {
            this.serverEndPoint = serverEndPoint;
            this.logger = logger;
        }

        

        public void Connect()
        {
            if (clientSocket == null)
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            if (isConnecting)
            {
                return;
            }
            isConnecting = true;
            connected = false;

            clientSocket.BeginConnect(serverEndPoint, ConnectCallback, clientSocket);
        }

        public void ClientUpdate()
        {
            //处理接收的消息
            for (int i = 0; i < disPatchCount; ++i)
            {
                DisPatchMsg();
            }
            
            //定时发送心跳
            long currentTime = NetTool.GetTimeStamp();
         
            if (currentTime - lastHeartTime >= heartDeltaTime)
            {
                lastHeartTime = currentTime;
                SendHeart();
            }
            

            ReallySendAllMsg();

            //logger.WriteLog("更新完成");
        }

        void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                logger.WriteLog("Socket Connect Succ ");
                serverSession.socket = socket;
                

                //开始接受消息
                reciveThread = new Thread(ClientReciveLoop);
                reciveThread.Start();

                isConnecting = false;
                connected = true;
            }
            catch (SocketException ex)
            {
                isConnecting = false;
                connected = false;

                logger.WriteLog("Socket Connect fail " + ex.ToString());
            }
        }

        public void SendMessage<T>(T msg)
        {
            SendMessage(msg, serverSession);
        }

        
        void ClientReciveLoop()
        {
            while (true)
            {
                if (serverSession.socket == null)
                {
                    CloseClient();
                    break;
                }

                if (serverSession.socket.Connected == false)
                {
                    CloseClient();
                    break;
                }

                

                ReciveSession(serverSession);
            }
        }

        void CloseClient()
        {
            connected = false;
        }

        void SendHeart()
        {
            logger.WriteLog("发送心跳");
            HeartMsg heartMsg = new HeartMsg();
            heartMsg.Time = (int)NetTool.GetTimeStamp();
            SendMessage(heartMsg);
        }
    }
}
