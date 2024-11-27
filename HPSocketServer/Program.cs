using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Http;
using TouchSocket.Sockets;
using TouchSocket.Core;

namespace HPSocketServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //CreateServer();
            SocketServer.RunServer().Wait();
            
            Console.ReadLine();
        }

        public static async void CreateServer()
        {
            TcpService service = new TcpService();
            service.Connecting = (client, e) => { return EasyTask.CompletedTask; };//有客户端正在连接
            service.Connected = (client, e) => { return EasyTask.CompletedTask; };//有客户端连接
            service.Closing = (client, e) => { return EasyTask.CompletedTask; };//有客户端正在断开
            service.Closed = (client, e) => {
                
                return EasyTask.CompletedTask; 
            };//有客户端断开连接
            service.Received = (client, e) =>
            {
                //Received information from the client
                string mes = e.ByteBlock.Span.ToString(Encoding.UTF8);
                Console.WriteLine($"Removed from {client.Id} Message received:");
                Console.WriteLine(mes);
                client.Send($"{client.Id}已收到消息");
                return EasyTask.CompletedTask; ;
            };
           
            await service.StartAsync(8899);//Start
        }
    }
}
