using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;

namespace _31._10_exam_2
{
    internal class Server
    {
        public List<User> Users = new List<User>();
        private String _filePath = "Users.txt";
        private TcpListener listener = new TcpListener(IPAddress.Any, 13000);
        private string _clientIPAdress = "127.0.0.1";

        List<ClientObject> clients = new List<ClientObject>();
        protected internal void RemoveConnection(string id)
        {
            ClientObject? client = clients.FirstOrDefault(c => c.Id == id);

            if (client != null) clients.Remove(client);
            client?.Close();
        }

        public Server()
        {
            Users = ReadFile();
            foreach (var user in Users)
                Console.WriteLine($"{user.Login} {user.Password} {user.IPAdress} {user.Port}");
            Console.WriteLine("-------------------------------------------------------");

            listener.Start();
        }

        public async Task Listen()
        {
            while (true)
            {
                using var tcpClient = await listener.AcceptTcpClientAsync();
                var serverClient = new ClientObject(tcpClient);

                clients.Add(serverClient);

                var msg = serverClient.Reader.ReadLine();
                ParseMsg(serverClient, msg);
                serverClient.Close();
            }
        }

        public void ParseMsg(ClientObject client, String msg)
        {
            Console.WriteLine($"Get msg: {msg}");
            var msgParts = msg.Split(";").ToList();
            var command = msgParts[0];
            var clientPort = int.Parse(msgParts[1]);
            Console.WriteLine($"Get msg: {msgParts[2]}");
            switch (command)
            {
                case "RegLogin": RegLogin(clientPort, msgParts[2]); break;
                case "RegPassword": RegPassword(clientPort, msgParts[2]); break;
                case "ConfirmPassword": ConfirmPassword(clientPort, msgParts[2]); break;
                case "EnterLogin": EnterLogin(clientPort, msgParts[2]); break;
                case "EnterPassword": EnterPassword(clientPort, msgParts[2]); break;

                case "InviteFriend": InviteFriend(clientPort, msgParts[2]); break;
                case "MsgForFriend": MsgForFriend(clientPort, msgParts[2], msgParts[3]); break;

                case "InviteGroupChat": InviteGroupChat(clientPort, msgParts[2]); break;
                case "SendMsgForGroup": SendMsgForGroup(clientPort, msgParts[2], msgParts[3]); break;

                case "SendFileToFriend":SendFileToFriend(clientPort, msgParts[2], msgParts[3]); break;
                case "SendFileToGroup": SendFileToGroup(clientPort, msgParts[2], msgParts[3]); break;
            }
        }

        public async Task Send(string msg, int port)
        {
            TcpClient tcpClient = new TcpClient(_clientIPAdress, port);
            var client = new ClientObject(tcpClient);
            client.Writer.WriteLine(msg);
            client.Close();
        }

        private string _regLogin;
        private void RegLogin(int clientPort, string login)
        {
            if (Users.Any(x => x.Login == login))
            {
                Send($"LoginAlreadyExist;{login}", clientPort);
            }
            else
            {
                _regLogin = login;
                Send($"RegPassword", clientPort);
            }
        }

        private string _regPassword;
        private void RegPassword(int clientPort, string password)
        {
            _regPassword = password;
            Send("ConfirmPassword", clientPort);
        }
        private void ConfirmPassword(int clientPort, string password)
        {
            if (password == _regPassword)
            {
                var newUser = new User(_clientIPAdress, clientPort.ToString(), _regLogin, _regPassword);
                Users.Add(newUser);
                WriteToFile();
                Send("RegSucces", clientPort);
            }
            else
            {
                Send("RegError", clientPort);
            }
        }

        private string _enterLogin;
        private void EnterLogin(int clientPort, string login)
        {
            if (Users.Any(x => x.Login == login))
            {
                _enterLogin = login;
                Send($"EnterPassword", clientPort);
            }
            else
            {
                Send($"LoginNotExist;{login}", clientPort);
            }
        }

        private void EnterPassword(int clientPort, string password)
        {
            var user = Users.FirstOrDefault(x => x.Login == _enterLogin);
            if (user.Password == password)
            {
                Send($"EnterSucces", clientPort);
            }
            else
            {
                Send($"EnterPassworError", clientPort);
            }
        }




        private void InviteFriend(int clientPort, string login)
        {
            var user = Users.FirstOrDefault((x => x.Login == login));
            if (user != null)
            {

                Send($"LoginFound;{login}", clientPort);
            }
            else
            {
                Send($"LogiNotnFound;{login}", clientPort);
            }
        }


        private void MsgForFriend(int clientPort, string login, string msg)
        {
            var user = Users.FirstOrDefault((x => x.Port == clientPort.ToString()));
            var friend = Users.FirstOrDefault((x => x.Login == login));
            var str = user.Login + "; " + msg;
            Send($"MsgForFriend;" + str, Int32.Parse(friend.Port));
        }


        private List<User> _friendsList = new List<User>();
        string _loginsStr = "";
        private void InviteGroupChat(int clientPort, string logins)
        {
            var lodinsList = logins.Split(";").ToList(); ;

            foreach (var login in lodinsList)
            {
                var user = Users.FirstOrDefault((x => x.Login == login));
                if (user != null)
                {
                    _friendsList.Add(user);
                    _loginsStr += user.Login;
                    _loginsStr += ";";
                }
            }
            if (_friendsList.Count > 0)
            {
                Send($"LoginFriendsFound;{_loginsStr}", clientPort);
            }
            else
            {
                Send($"LogiNotnFound;{logins}", clientPort);
            }
        }



        private void SendMsgForGroup(int clientPort, string logins, string msg)
        { 
        var lodinsList = logins.Split(";").ToList(); ;

            foreach (var login in lodinsList)
            {
                var user = Users.FirstOrDefault((x => x.Port == clientPort.ToString()));
                var friend = Users.FirstOrDefault((x => x.Login == login));
                var str = user.Login + "; " + msg + "; " + logins;
                Send($"MsgForGroup;" + str, Int32.Parse(friend.Port));
            }
         }

        public List<User> ReadFile()
        {
            var result = new List<User>();
            if (File.Exists(_filePath))
                using (StreamReader reader = new StreamReader(_filePath))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(';');
                        result.Add(new User(parts[0], parts[1], parts[2], parts[3]));
                    }
                }
            return result;
        }

        private void MsgForFriendAboutFile(int clientPort, string login, string savePath)
        {
           
                var user = Users.FirstOrDefault((x => x.Port == clientPort.ToString()));
                var friend = Users.FirstOrDefault((x => x.Login == login));
                var FileName = savePath.Substring(savePath.LastIndexOf("\\"));
                var str = user.Login + "; " + "отправлен файл;" + FileName;
                Send($"MsgForFriendAboutFile;{str}", Int32.Parse(friend.Port));
            
        }
        private void MsgForGroupAboutFile(int clientPort, string logins, string savePath)
        {
            var lodinsList = logins.Split(";").ToList(); ;

            foreach (var login in lodinsList)
            {
                var user = Users.FirstOrDefault((x => x.Port == clientPort.ToString()));
                var friend = Users.FirstOrDefault((x => x.Login == login));
                var FileName = savePath.Substring(savePath.LastIndexOf("\\"));
                var str = user.Login + "; " + "отправлен файл" + logins + FileName;
                Send($"MsgForGroupAboutFile;{str}", Int32.Parse(friend.Port));
            }
        }

        private async Task SendFileToFriend(int clientPort, string login, string savePath)
        {
            MsgForFriendAboutFile(clientPort, login, savePath);


            await ReceiveFile(savePath);

             var friend = Users.FirstOrDefault(x => x.Login == login);
                if (friend != null)
                {
                    await SendFileToClient(Int32.Parse(friend.Port), savePath);
                }
            
        }


        private async Task SendFileToGroup(int clientPort, string logins, string savePath)
        {
            MsgForGroupAboutFile(clientPort, logins, savePath);


            await ReceiveFile(savePath);

            
            var lodinsList = logins.Split(";").ToList(); ;

            foreach (var login in lodinsList)
            {
                var friend = Users.FirstOrDefault(x => x.Login == login);
                if (friend != null)
                {
                    await SendFileToClient(Int32.Parse(friend.Port), savePath);
                }
            }
        }

        private async Task ReceiveFile(string savePath)
        {
            using var tcpClient = await listener.AcceptTcpClientAsync();
            var serverClient = new ClientObject(tcpClient);

            clients.Add(serverClient);

            var fileName = savePath.Substring(savePath.LastIndexOf('\\') + 1);

           
            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await serverClient.Reader.BaseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }

                    Console.WriteLine($"Файл успешно получен и сохранён как {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении файла: {ex.Message}");
            }
        }

      
        private async Task SendFileToClient(int port, string filePath)
        {
            TcpClient tcpClient = new TcpClient(_clientIPAdress, port);
            var client = new ClientObject(tcpClient);
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                   
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await client.Writer.BaseStream.WriteAsync(buffer, 0, bytesRead);
                    }

                    Console.WriteLine($"Файл {filePath} успешно отправлен клиенту ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке файла клиенту: {ex.Message}");
            }
        }

        public void WriteToFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    var fs = File.Create(_filePath);
                    fs.Close();
                }

                using (StreamWriter writer = new StreamWriter(_filePath, false))
                {
                    foreach (var user in Users)
                    {
                        writer.WriteLine(String.Join(";", user.IPAdress, user.Port, user.Login, user.Password));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            }
        }
    }

    class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        protected internal StreamWriter Writer { get; }
        protected internal StreamReader Reader { get; }

        internal int Port => int.Parse(tcpClient.Client.RemoteEndPoint.ToString().Split(":")[1]);

        public string Name = "";

        internal TcpClient tcpClient;


        public ClientObject(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            var stream = this.tcpClient.GetStream();

            Reader = new StreamReader(stream);

            Writer = new StreamWriter(stream) { AutoFlush = true };
        }
        protected internal void Close()
        {
            Writer.Close();
            Reader.Close();
            tcpClient.Close();
        }
    }

    public class User 
    { 
        public String IPAdress { get; set; }
        public string Port { get; set; }
        public String Login { get; set; }
        public String Password { get; set; }

        public User(string iPAdress, string port, string login, string password)
        {
            IPAdress = iPAdress;
            Port = port;
            Login = login;
            Password = password;
        }
    }
}
