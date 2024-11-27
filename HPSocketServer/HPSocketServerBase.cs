using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPSocketServer
{
    internal class HPSocketServerBase
    {
        public async void CreateServer(string host, int port) 
        {
            //int port = 5000; // 监听的端口号
            IPAddress localAddr = IPAddress.Any; // 监听所有本地网络接口

            // 创建 TcpListener
            TcpListener server = new TcpListener(localAddr, port);

            try
            {
                // 启动服务端
                server.Start();
                Console.WriteLine($"异步服务端已启动，监听端口：{port}");

                while (true)
                {
                    // 等待客户端连接
                    Console.WriteLine("等待客户端连接...");
                    TcpClient client = await server.AcceptTcpClientAsync();
                    Console.WriteLine("客户端已连接！");

                    // 异步处理客户端通信（不阻塞主线程）
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
            }
            finally
            {
                server.Stop();
                Console.WriteLine("服务端已停止。");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead;

                try
                {
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        // 接收客户端数据
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"收到客户端数据: {receivedData}");

                        // 发送响应给客户端
                        string response = $"服务端已收到: {receivedData}";
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseData, 0, responseData.Length);
                        Console.WriteLine("响应已发送给客户端。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理客户端时发生异常: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("客户端连接已关闭。");
                }
            }
        }

    }
}

