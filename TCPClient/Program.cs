
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        var enc1251 = CodePagesEncodingProvider.Instance.GetEncoding(1251);
        Console.InputEncoding = enc1251;
        Console.OutputEncoding = enc1251;
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        int port = 13000;
        TcpClient client = new TcpClient();
        client.Connect(ip, port);
        NetworkStream ns = client.GetStream();
        Thread thread = new Thread(o => ReceiveData((TcpClient)o));
        thread.Start(client);
        string goodbyeString = "пока";
        string s;
        while (!string.IsNullOrEmpty((s = Console.ReadLine())))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);
            if (s == goodbyeString)
                break;
        }
        client.Client.Shutdown(SocketShutdown.Both);
        thread.Join();
        ns.Close();
        client.Close();
    }

    static void ReceiveData(TcpClient client)
    {
        NetworkStream ns = client.GetStream();
        byte[] receivedBytes = new byte[1024];
        int byteCount;

        while ((byteCount = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
        {
            Console.WriteLine(Encoding.UTF8.GetString(receivedBytes, 0, byteCount));            
        }
    }
}