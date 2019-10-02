using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatBot
{
    class ChatClient
    {
        public string Name { get; set; }
        public readonly TcpClient Client;
        public ChatClient(TcpClient tcpClient, string name = " ")
        {
            Name = name;
            Client = tcpClient;
        }
    }
}
