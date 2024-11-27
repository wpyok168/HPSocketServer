using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientDemo
{
    internal class AsyncFileSenderJsonClient
    {
        static async Task Main()
        {
            string server = "127.0.0.1"; // 服务端地址
            int port = 5000; // 服务端端口

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(server, port);
                    Console.WriteLine("已连接到服务端！");
                    NetworkStream stream = client.GetStream();

                    while (true)
                    {
                        Console.WriteLine("\n选择操作:");
                        Console.WriteLine("1. 发送文字");
                        Console.WriteLine("2. 发送文件");
                        Console.WriteLine("0. 退出");
                        Console.Write("请输入: ");
                        string option = Console.ReadLine();

                        if (option == "1")
                        {
                            // 发送文字
                            Console.Write("请输入要发送的文字: ");
                            string text = Console.ReadLine();

                            var message = new JsonMessage { Type = "text", Content = text };
                            string json = System.Text.Json.JsonSerializer.Serialize(message);
                            byte[] messageBytes = Encoding.UTF8.GetBytes(json);
                            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                            // 接收服务端响应
                            byte[] buffer = new byte[1024];
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var response = System.Text.Json.JsonSerializer.Deserialize<JsonMessage>(responseJson);
                            Console.WriteLine($"服务端响应: {response?.Content}");
                        }
                        else if (option == "2")
                        {
                            // 发送文件
                            Console.Write("请输入文件路径: ");
                            string filePath = Console.ReadLine();

                            if (!File.Exists(filePath))
                            {
                                Console.WriteLine("文件不存在！");
                                continue;
                            }

                            string fileName = Path.GetFileName(filePath);
                            long fileSize = new FileInfo(filePath).Length;

                            // 发送文件头
                            var fileInfo = new JsonMessage { Type = "file", FileName = fileName, FileSize = fileSize };
                            string fileInfoJson = System.Text.Json.JsonSerializer.Serialize(fileInfo);
                            byte[] fileInfoBytes = Encoding.UTF8.GetBytes(fileInfoJson);
                            await stream.WriteAsync(fileInfoBytes, 0, fileInfoBytes.Length);

                            // 接收续传偏移量
                            byte[] buffer = new byte[1024];
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            string resumeJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var resumeInfo = System.Text.Json.JsonSerializer.Deserialize<JsonMessage>(resumeJson);
                            long offset = resumeInfo?.Offset ?? 0;
                            Console.WriteLine($"从偏移量 {offset} 开始发送文件。");

                            // 从偏移量开始发送文件
                            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                fileStream.Seek(offset, SeekOrigin.Begin);

                                int readBytes;
                                while ((readBytes = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await stream.WriteAsync(buffer, 0, readBytes);
                                }
                            }

                            Console.WriteLine("文件发送完成！");
                        }
                        else if (option == "0")
                        {
                            Console.WriteLine("退出程序。");
                            break;
                        }
                        else
                        {
                            Console.WriteLine("无效选项！");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"客户端异常: {ex.Message}");
            }
        }
    }
    // 定义 JSON 消息格式
    public class JsonMessage
    {
        public string Type { get; set; } // 消息类型: "text", "file", "resume"
        public string Content { get; set; } // 文本内容
        public string FileName { get; set; } // 文件名
        public long? FileSize { get; set; } // 文件大小
        public long? Offset { get; set; } // 续传偏移量
    }
}
