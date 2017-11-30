using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ts8.Data;

namespace ts8.Server {
    class Server {
        private const int listenPort = 6100;
        private const int timeSenderPort = 11000;
        private const int playerLimit = 1;
        private static int time;
        private static int tempTime = 0;
        private static bool gameRunning = false;
        private static int numberToGuess;
        private static UdpClient _listener;

        private static Timer timer;
        private static UdpClient _timeSender;

        private static IPEndPoint _ipEndPoint;
        private static IPEndPoint _ipEndPointTimeSender;

        private static Dictionary<IPEndPoint, PlayerData> _players;


        private static void Main(string[] args) {
            SetupServer();
            RegisterUsers();
            SendStartMessage();
            StartGame();
        }

        private static void SetupServer() {
            _ipEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            //_ipEndPointTimeSender = new IPEndPoint(IPAddress.Any, timeSenderPort);
            _listener = new UdpClient(_ipEndPoint);
            _timeSender = new UdpClient(timeSenderPort);
            _players = new Dictionary<IPEndPoint, PlayerData>();
        }

        private static void RegisterUsers() {
            while (_players.Count < playerLimit) {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] recvMsg = _listener.Receive(ref sender);
                Data.Packet packet = Data.Packet.Deserialize(Encoding.UTF8.GetString(recvMsg));
                Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);

                ProcessData(packet, sender);
            }
        }

        private static void SendStartMessage() {
            foreach (var playerData in _players) {
                Data.Packet packet = new Data.Packet(playerData.Value.SessionID, 0, OD_Enum.ACK, OP_Enum.START);
                string stringToSend = packet.Serialize();
                byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                _listener.Send(bytesToSend, bytesToSend.Length, playerData.Value.PlayerEndPoint);
            }
        }

        private static void StartGame() {
            Console.WriteLine(CalculateTime());
            time = CalculateTime();
            //time = 3;
            gameRunning = true;
            numberToGuess = HelperData.RandomInt(0, 255);
            //numberToGuess = 10;
            Console.WriteLine("Number to guess: {0}", numberToGuess);
            StartClientThreads();
            timer = new Timer(SubstractTime, 5, 0, 1000);


        }

        private static int CalculateTime() {
            int sessionIDSum = 0;
            foreach (var playerData in _players) {
                sessionIDSum += playerData.Value.SessionID;
            }
            return ((sessionIDSum * 99) % 100) + 30;
        }

        private static void SubstractTime(object state) {
            if (time > 0) {
                Console.WriteLine(time); ;
                if (tempTime < 10) {
                    tempTime++;
                    Console.WriteLine(tempTime);
                }
                if (tempTime == 10) {
                    Thread thread = new Thread(SendTime);
                    thread.Start(time);
                    tempTime = 0;
                }
                time--;
            } else if (time == 0) {
                gameRunning = false;
                foreach (var playerData in _players) {
                    try {
                        Data.Packet packetToSend = new Data.Packet(playerData.Value.SessionID, 0, OD_Enum.TIME_OUT,
                            OP_Enum.TIME);
                        string stringToSend = packetToSend.Serialize();
                        byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                        _listener.Send(bytesToSend, bytesToSend.Length, playerData.Value.PlayerEndPoint);
                    } catch (Exception e) {
                        Console.WriteLine("Client disconected: {0}", playerData.Value.SessionID);
                    }
                }
                _listener.Close();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private static void SendTime(object t) {
            int timeToSend = (int)t;
            foreach (var playerData in _players) {
                Console.WriteLine("Wysyłam czas do: {0}", playerData.Key.ToString());
                Data.Packet packet = new Data.Packet(playerData.Value.SessionID, timeToSend, OD_Enum.NULL, OP_Enum.TIME);
                string stringToSend = packet.Serialize();
                byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                _listener.Send(bytesToSend, bytesToSend.Length, playerData.Key);
            }
        }


        public static void DataIN(object ep) {
            while (gameRunning) {
                IPEndPoint sender = (IPEndPoint)ep;
                try {
                    byte[] recByte = _listener.Receive(ref sender);
                    string recString = Encoding.UTF8.GetString(recByte);
                    Data.Packet packet = Data.Packet.Deserialize(recString);
                    Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);

                    ProcessData(packet, sender);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void StartClientThreads() {
            foreach (var playerData in _players) {
                playerData.Value.StartThread();
            }
            //Thread thread = new Thread(SendTime);
            //thread.Start();
        }

        private static void ProcessData(object p, object ep) {
            Data.Packet packet = (Data.Packet)p;
            IPEndPoint endPoint = (IPEndPoint)ep;
            Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);

            if (packet.OP == OP_Enum.REGISTER && packet.OD == OD_Enum.REQUEST) {
                Packet ackPacket = new Packet(packet.ID, OD_Enum.NULL, OP_Enum.ACK);
                string ackString = ackPacket.Serialize();
                byte[] bytesAck = Encoding.UTF8.GetBytes(ackString);
                _listener.Send(bytesAck, bytesAck.Length, endPoint);
                Console.Write("Operation: {0}, answer: {1}, id: {2}, data: {3}", packet.OP, packet.OD, packet.ID, packet.Data);
                Register(packet, endPoint);
            }
            if (packet.OP == OP_Enum.GUESS) {
                Packet ackPacket = new Packet(packet.ID, OD_Enum.NULL, OP_Enum.ACK);
                string ackString = ackPacket.Serialize();
                byte[] bytesAck = Encoding.UTF8.GetBytes(ackString);
                _listener.Send(bytesAck, bytesAck.Length, endPoint);
                Guessing(packet, endPoint);
            }
        }



        private static void Register(Data.Packet packet, IPEndPoint endPoint) {
            if (!_players.ContainsKey(endPoint)) {
                var id = HelperData.RandomInt(0, 255);
                _players.Add(endPoint, new PlayerData(endPoint, id));
                Data.Packet packetToSend = new Data.Packet(id, 0, OD_Enum.ACK, OP_Enum.REGISTER);
                string stringToSend = packetToSend.Serialize();
                byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                _listener.Send(bytesToSend, bytesToSend.Length, endPoint);
            }
        }

        private static void Guessing(Data.Packet packet, IPEndPoint endPoint) {
            if (packet.Data == numberToGuess) {
                Data.Packet packetToSend = new Data.Packet(packet.ID, 0, OD_Enum.GUESSED, OP_Enum.GUESS);
                string stringToSend = packetToSend.Serialize();
                byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                _listener.Send(bytesToSend, bytesToSend.Length, endPoint);
                gameRunning = false;
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                foreach (var playerData in _players) {
                    if (!playerData.Value.PlayerEndPoint.Equals(endPoint)) {
                        Data.Packet packetToSendForNotGuessed = new Data.Packet(playerData.Value.SessionID, 0,
                            OD_Enum.NULL, OP_Enum.SUMMARY);
                        string stringToSendForNotGuessed = packetToSendForNotGuessed.Serialize();
                        byte[] bytesToSendForNotGuessed = Encoding.UTF8.GetBytes(stringToSendForNotGuessed);
                        _listener.Send(bytesToSendForNotGuessed, bytesToSendForNotGuessed.Length, playerData.Key);
                    }
                }
                Console.ReadLine();
                gameRunning = false;
                _listener.Close();
            } else {
                Data.Packet packetToSend = new Data.Packet(packet.ID, 0, OD_Enum.NOT_GUESSED, OP_Enum.GUESS);
                string stringToSend = packetToSend.Serialize();
                byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                _listener.Send(bytesToSend, bytesToSend.Length, endPoint);
            }
        }

        class PlayerData {
            public int SessionID { get; set; }
            public IPEndPoint PlayerEndPoint { get; set; }
            private Thread _playerThread;

            public PlayerData(IPEndPoint ep, int id) {
                PlayerEndPoint = ep;
                SessionID = id;
                _playerThread = new Thread(Server.DataIN);
            }

            public void StartThread() {
                _playerThread.Start(PlayerEndPoint);
            }
        }

        class ThreadObject {
            public IPEndPoint EndPoint { get; set; }
            public Data.Packet Packet { get; set; }

            public ThreadObject(IPEndPoint ep, Data.Packet p) {
                EndPoint = ep;
                Packet = p;
            }
        }
    }
}
