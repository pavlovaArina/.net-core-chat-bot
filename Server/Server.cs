using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChatBot
{
    class Server
    {
        static readonly List<ChatClient> clients = new List<ChatClient>();
        private TcpListener server = null;
        private string getAllUsersCommand = "кто здесь";

        private static Dictionary<string, string> questionsAndAnswers = new Dictionary<string, string>()
        {
            ["как дела"] = "Хорошо",
            ["чай или кофе"] = "Кофе",
            ["кошки или собаки"] = "Кошки",
            ["табы или пробелы"] = "Пффф",
            ["лыжи или сноуборд"] = "Сноуборд",
            ["сложно отвечать на вопросы"] = "Очень"
        };

        private readonly string goodbyeString = "пока";
        public Server(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            StartListener();
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    var client = new ChatClient(server.AcceptTcpClient());
                    clients.Add(client);
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        public void HandleDeivce(Object obj)
        {
            ChatClient chatClient = (ChatClient)obj;
            var stream = chatClient.Client.GetStream();
            string helloString = "Привет, как тебя зовут?";
            Byte[] hello = System.Text.Encoding.UTF8.GetBytes(helloString);
            stream.Write(System.Text.Encoding.UTF8.GetBytes(helloString), 0, hello.Length);
            Byte[] bytes = new Byte[256];
            int i;
            bool isFirstAnswer = true;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var question = Encoding.UTF8.GetString(bytes, 0, i);
                    Regex rgx = new Regex("[^a-zA-Zа-яА-Я0-9 -]");
                    question = rgx.Replace(question.ToLower(), "");
                    if (question == goodbyeString) break;
                    if (!isFirstAnswer)
                    {
                        if (question == getAllUsersCommand)
                        {
                            foreach (var client in clients)
                            {
                                Byte[] reply = System.Text.Encoding.UTF8.GetBytes(client.Name.First().ToString().ToUpper() + client.Name.Substring(1));
                                stream.Write(reply, 0, reply.Length);
                            }
                        }
                        else
                        {
                            string answer = questionsAndAnswers.ContainsKey(question)
                                ? questionsAndAnswers[question]
                                : "whatever";
                            Byte[] reply = System.Text.Encoding.UTF8.GetBytes(answer);
                            stream.Write(reply, 0, reply.Length);
                        }
                    }
                    else
                    {
                        isFirstAnswer = false;
                        chatClient.Name = question;
                        string answer = "А я бот Борис и меня можно спросить о самых важных вещах:\r\n";
                        foreach (var qa in questionsAndAnswers)
                        {
                            answer += qa.Key.First().ToString().ToUpper() + qa.Key.Substring(1) + "?\r\n";
                        }
                        answer += "напиши \"кто здесь\", чтобы получить список присоединенных пользователей\r\n";
                        Byte[] reply = System.Text.Encoding.UTF8.GetBytes(answer);
                        stream.Write(reply, 0, reply.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                chatClient.Client.Close();
            }
            clients.Remove(chatClient);
            chatClient.Client.Client.Shutdown(SocketShutdown.Both);
            chatClient.Client.Close();
        }
    }
}