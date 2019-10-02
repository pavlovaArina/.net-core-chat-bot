using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly List<ChatClient> Clients = new List<ChatClient>();
        private TcpListener server = null;
        private static Dictionary<string, string> questionsAndAnswers = new Dictionary<string, string>();
        private static Dictionary<string, string> commands = new Dictionary<string, string>();

        private readonly string goodbyeString = "пока";
        private readonly string getAllUsersString = "кто здесь";
        private readonly string helloString = "А я бот Борис и меня можно спросить о самых важных вещах:";

        public Server(string ip, int port)
        {
            var lines = File.ReadAllLines(@"questions.txt").Select(x => x.Split(';')).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                questionsAndAnswers.Add(lines[i][0], lines[i][1]);
            }
            lines = File.ReadAllLines(@"commands.txt").Select(x => x.Split(';')).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                commands.Add(lines[i][0], lines[i][1]);
            }
            IPAddress ipAddress = IPAddress.Parse(ip);
            server = new TcpListener(ipAddress, port);
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
                    Clients.Add(client);
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        public void HandleClient(Object obj)
        {
            ChatClient chatClient = (ChatClient)obj;
            var stream = chatClient.Client.GetStream();
            string helloString = "Привет, как тебя зовут?";
            Byte[] hello = System.Text.Encoding.UTF8.GetBytes(helloString);
            stream.Write(System.Text.Encoding.UTF8.GetBytes(helloString), 0, hello.Length);
            Byte[] bytes = new Byte[256];
            bool isFirstAnswer = true;
            try
            {
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var question = Encoding.UTF8.GetString(bytes, 0, i);
                    Regex rgx = new Regex("[^a-zA-Zа-яА-Я0-9 -]");
                    question = Regex.Replace(rgx.Replace(question, ""), @"\s+", " ").Trim().ToLower();
                    if (question == goodbyeString) break;
                    CreateAndSendAnswer(chatClient, question, ref isFirstAnswer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                Clients.Remove(chatClient);
                chatClient.Client.Client.Shutdown(SocketShutdown.Both);
                chatClient.Client.Close();
            }
        }

        private void CreateAndSendAnswer(ChatClient chatClient, string question, ref bool isFirstAnswer)
        {
            var stream = chatClient.Client.GetStream();
            if (!isFirstAnswer)
            {
                if (question == getAllUsersString)
                {
                    foreach (var client in Clients) Answer(stream, client.Name);
                }
                else
                {
                    string answer = questionsAndAnswers.ContainsKey(question)
                        ? questionsAndAnswers[question]
                        : "whatever";
                    Answer(stream, answer);
                }
            }
            else
            {
                isFirstAnswer = false;
                chatClient.Name = question.First().ToString().ToUpper() + question.Substring(1);
                string answer = helloString + Environment.NewLine;
                answer += string.Join(Environment.NewLine, questionsAndAnswers.Keys.Select(key => key.First().ToString().ToUpper() + key.Substring(1) + "?"));
                answer += Environment.NewLine + Environment.NewLine;
                answer += string.Join(Environment.NewLine, commands.ToList().Select(pair => $"Введи {pair.Key}, чтобы {pair.Value}"));
                Answer(stream, answer);
            }
        }

        private void Answer(NetworkStream stream, string answer)
        {
            Byte[] reply = System.Text.Encoding.UTF8.GetBytes(answer);
            stream.Write(reply, 0, reply.Length);
        }
    }
}