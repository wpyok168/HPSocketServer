using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HPSocketServer
{

    class SocketServer
    {
        public static async Task RunServer(int port = 8899)
        {
            //int port = 5000; // 监听端口
            TcpListener server = new TcpListener(IPAddress.Any, port);

            try
            {
                server.Start();
                Console.WriteLine($"服务端已启动，监听端口: {port}");

                while (true)
                {
                    Console.WriteLine("等待客户端连接...");
                    TcpClient client = await server.AcceptTcpClientAsync();
                    Console.WriteLine($"客户端连接成功: {((IPEndPoint)client.Client.RemoteEndPoint)?.Address}");

                    // 为每个客户端创建独立任务
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务端异常: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];

                try
                {
                    while (true)
                    {
                        // 接收 JSON 消息
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string jsonMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        JsonMessage message = null;
                        try
                        {
                            message = System.Text.Json.JsonSerializer.Deserialize<JsonMessage>(jsonMessage);
                        }
                        catch (Exception)
                        {

                        }

                        if (message == null)
                        {
                            Console.WriteLine("收到无效 JSON 消息，忽略处理。");
                            continue;
                        }

                        switch (message.Type)
                        {
                            case "text":
                                // 处理文字交互
                                Console.WriteLine($"客户端消息: {message.Content}");

                                // 回应客户端
                                var response = new JsonMessage { Type = "text", Content = $"服务端已收到: {message.Content}" };
                                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                                break;

                            case "file":
                                // 处理文件传输
                                string fileName = message.FileName ?? "unknown";
                                //long fileSize1 = message.FileSize ?? 0L;
                                long fileSize = message.FileSize.GetHashCode() >0 ? message.FileSize : 0;
                                

                                Console.WriteLine($"开始接收文件: {fileName}, 大小: {fileSize} 字节");

                                // 确保保存目录存在
                                string savePath = Path.Combine("ReceivedFiles", fileName);
                                Directory.CreateDirectory("ReceivedFiles");

                                long existingFileSize = 0;
                                if (File.Exists(savePath))
                                {
                                    existingFileSize = new FileInfo(savePath).Length;
                                }

                                // 通知客户端续传偏移量
                                var resumeInfo = new JsonMessage { Type = "resume", Offset = existingFileSize };
                                string resumeJson = System.Text.Json.JsonSerializer.Serialize(resumeInfo);
                                byte[] resumeBytes = Encoding.UTF8.GetBytes(resumeJson);
                                await stream.WriteAsync(resumeBytes, 0, resumeBytes.Length);

                                // 接收文件内容
                                using (FileStream fileStream = new FileStream(savePath, FileMode.Append, FileAccess.Write))
                                {
                                    long totalBytesReceived = existingFileSize;

                                    while (totalBytesReceived < fileSize)
                                    {
                                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                        if (bytesRead == 0) break;

                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        totalBytesReceived += bytesRead;

                                        Console.WriteLine($"已接收 {totalBytesReceived}/{fileSize} 字节");
                                    }
                                }

                                Console.WriteLine($"文件接收完成: {savePath}");
                                break;

                            default:
                                Console.WriteLine($"未知消息类型: {message.Type}");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"客户端通信异常: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine($"客户端已断开连接: {((IPEndPoint)client.Client.RemoteEndPoint)?.Address}");
                }
            }
        }
    }
        // 定义 JSON 消息格式
        public class JsonMessage
    {
        public string Type { get; set; } // 消息类型: "text", "file", "resume"
        public string Content { get; set; } // 文本内容
        public string FileName { get; set; } // 文件名
        public long FileSize { get; set; } // 文件大小
        public long Offset { get; set; } // 续传偏移量
    }
}
