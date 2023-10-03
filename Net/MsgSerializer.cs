using Google.Protobuf;
using LeafNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//TODO 优化GC
namespace LeafNet
{
    public static class MsgSerializer
    {
        static byte[] EncodeInt(int value)
        {
            byte[] bytes = new byte[2];
            uint v = (uint)value;
            bytes[0] = (byte)(v >> 16);
            bytes[1] = (byte)(v & 0x0000ffff);
            return bytes;
        }

        static int DecodeInt(byte[] data)
        {
            if (data.Length != 2)
            {
                throw new IndexOutOfRangeException("DecodeInt Failed, data.Lenth is not 2");
            }
            uint v = 0;
            v = (uint)data[0];
            v <<= 16;
            v |= (uint)data[1];
            return (int)v;
        }


        
        /// <summary>
        /// 写入一条消息到缓冲区
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="msgName"></param>
        /// <param name="msg"></param>
        public static void EncodeMsg(this ByteArray byteArray, string msgName, IMessage msg)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(msgName);
            int nameLen = nameBytes.Length;
            byte[] nameHead = EncodeInt(nameLen);
            byte[] msgBody;
            using (MemoryStream ms = new MemoryStream())
            {
                msg.WriteTo(ms);
                msgBody = ms.ToArray();
            }
            int msgLen = 2 + 2 + nameLen + msgBody.Length;
            byte[] msgHead = EncodeInt(msgLen);
            byte[] msgBytes = new byte[msgLen];


            // 将各个部分拷贝到消息字节数组中
            int offset = 0;
            Array.Copy(msgHead, 0, msgBytes, offset, 2);
            offset += 2;
            Array.Copy(nameHead, 0, msgBytes, offset, 2);
            offset += 2;
            Array.Copy(nameBytes, 0, msgBytes, offset, nameLen);
            offset += nameLen;
            Array.Copy(msgBody, 0, msgBytes, offset, msgBody.Length);

            byteArray.Write(msgBytes, 0, msgLen);
        }

        /// <summary>
        /// 如果可以读取，则读取一条消息
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="msgName"></param>
        /// <param name="msg"></param>
        /// <param name="msgParserMapper"></param>
        /// <returns></returns>
        public static bool DecodeMsg(this ByteArray byteArray, out string msgName, out IMessage msg, ClassMapper<MessageParser> msgParserMapper)
        {
            msgName = null;
            msg = null;

            //如果长度小于2则退出
            if (byteArray.length <= 2) {
                return false;
            }

            //如果长度不足，退出
            byte[] msgHead = new byte[]
            {
                byteArray.bytes[byteArray.readIdx],
                byteArray.bytes[byteArray.readIdx + 1]
            };
            int msgLen = DecodeInt(msgHead);
            if (msgLen < byteArray.length)
            {
                return false;
            }

            //正式读取
            byte[] msgBytes = new byte[msgLen];
            byteArray.Read(msgBytes, 0, msgLen);
            int offset = 0;
            byte[] nameHead = new byte[]
            {
                msgBytes[2],
                msgBytes[3],
            };
            int nameLen = DecodeInt(nameHead);
            offset = 4;
            byte[] msgNameBytes = new byte[nameLen];
            Array.Copy(msgBytes, offset, msgNameBytes, 0, nameLen);
            msgName = Encoding.UTF8.GetString(msgNameBytes);
            offset += nameLen;
            byte[] msgBody = new byte[msgLen - 2 - 2 - nameLen];
            Array.Copy(msgBytes, offset, msgBody, 0, msgLen - offset);

            MessageParser messageParser = msgParserMapper.GetParser(msgName);
            msg = messageParser.ParseFrom(msgBody);
            return true;
        }
    }
}
