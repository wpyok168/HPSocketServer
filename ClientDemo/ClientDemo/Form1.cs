using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace ClientDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LJServer();
        }

        TcpClient tcpClient = new TcpClient();
        private async void LJServer()
        {
            
            tcpClient.Connected = (client, e) => { return EasyTask.CompletedTask; };//Successfully connected to the server
            tcpClient.Closed = (client, e) => { return EasyTask.CompletedTask; };//Disconnect from the server, which is not triggered when the connection is unsuccessful.
            tcpClient.Received = (client, e) =>
            {
                //Information is received from the server
                string mes = e.ByteBlock.Span.ToString(Encoding.UTF8);
                Console.WriteLine($"Message received: {mes}");
                return EasyTask.CompletedTask;
            };

            await tcpClient.ConnectAsync("127.0.0.1:8899");
            await tcpClient.SendAsync("Hello");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await tcpClient.SendAsync("{\"Type\" :\"text\", \"Content\" :\"hello 8899\"}");
        }
    }
}
