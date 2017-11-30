using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ts8.Data;

namespace ts8.Client {
    class Program {
        private const int listenPort = 6100;
        private const int timeSenderPort = 4000;
        private static UdpClient _udpClient;
        private static UdpClient _timeClient;
        private static IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 6100);
        private static bool gameRunning = true;
        static void Main(string[] args) {
            bool registered = false;
            SetupClient();
            do {
                Console.WriteLine("Wpisz register, aby się zarejestrować:");
                var msg = Console.ReadLine();
                if (msg.ToLower().Equals("register")) {

                    Data.Packet packet = new Data.Packet(OD_Enum.REQUEST, OP_Enum.REGISTER);
                    string stringToSend = packet.Serialize();
                    byte[] msgToSend = Encoding.UTF8.GetBytes(stringToSend);
                    _udpClient.Send(msgToSend, msgToSend.Length);

                    byte[] recACK = _udpClient.Receive(ref Program.remoteEndPoint);
                    string ackString = Encoding.UTF8.GetString(recACK);
                    Packet ackPacket = Packet.Deserialize(ackString);
                    byte[] recvMessage = _udpClient.Receive(ref Program.remoteEndPoint);
                    string recvMessageString = Encoding.UTF8.GetString(recvMessage);
                    Data.Packet recPacket = Data.Packet.Deserialize(recvMessageString);
                    Console.WriteLine("Zarejestrowano, ID: {0}, data: {1}, answer: {2}, operation: {3}", recPacket.ID, recPacket.Data, recPacket.OD, recPacket.OP);
                    registered = true;
                } else {
                    Console.WriteLine("Niepoprawna komenda");
                    registered = false;
                }

            } while (!registered);
            Thread thread = new Thread(DataIN);
            thread.Start();
        }

        private static void SetupClient() {
            //IPAddress endpointIPAddress = IPAddress.Parse("172.20.10.8"); //tutaj wpisać adres serwera
            IPAddress endpointIPAddress = IPAddress.Parse("192.168.0.1"); //tutaj wpisać adres serwera

            _udpClient = new UdpClient();
            _udpClient.Connect(endpointIPAddress, listenPort);


            _timeClient = new UdpClient();
            _timeClient.Connect(endpointIPAddress, timeSenderPort);
            //_timeClient.Connect(endpointIPAddress, timeSenderPort);
        }

        private static void DataIN() {
            while (gameRunning) {
                try {
                        byte[] recBuff = _udpClient.Receive(ref remoteEndPoint);
                        string recString = Encoding.UTF8.GetString(recBuff);
                        if (recBuff.Length > 0) {
                            Data.Packet recvPacket = Data.Packet.Deserialize(recString);
                            Packet packet = new Packet(recvPacket.ID, OD_Enum.NULL, OP_Enum.ACK);
                            string serializedString = packet.Serialize();
                            byte[] bytesAck = Encoding.ASCII.GetBytes(serializedString);
                            _udpClient.Send(bytesAck, bytesAck.Length);
                            Thread dataManagerThread = new Thread(DataManager);
                            dataManagerThread.Start(recvPacket);
                            Console.WriteLine("ID: {0} OD: {1} OP: {2} DATA: {3}", recvPacket.ID, recvPacket.OD, recvPacket.OP, recvPacket.Data);
                        }
                } catch (Exception e) {
                    Console.WriteLine("Server disconnected");
                }
            }
        }

        private static void DataTimeIn() {
            while (gameRunning) {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, timeSenderPort);
                byte[] recBuff = _timeClient.Receive(ref Program.remoteEndPoint);
                var recMsg = Encoding.UTF8.GetString(recBuff);
                Console.WriteLine("Wiadomosc z serwera: {0}", recMsg);
            }
        }
        private static void DataManager(object p) {
            Data.Packet packet = (Data.Packet)p;

            if (packet.OP == OP_Enum.START) {
                Console.WriteLine("Gra wystartowała!");
                InputNumber(packet);
            }

            if (packet.OP == OP_Enum.GUESS && packet.OD == OD_Enum.GUESSED) {
                Console.WriteLine("Wygrałeś!");
                Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);
                gameRunning = false;
                _udpClient.Close();
                Console.ReadLine();
            }
            if (packet.OP == OP_Enum.GUESS && packet.OD == OD_Enum.NOT_GUESSED) {
                Console.WriteLine("Nie zgadłeś!");
                InputNumber(packet);
            }
            if (packet.OP == OP_Enum.SUMMARY) {
                Console.WriteLine("Gra zakończona nie wygrałeś.");
                gameRunning = false;
                _udpClient.Close();
                Console.ReadLine();
            }
            if (packet.OP == OP_Enum.TIME && packet.OD == OD_Enum.NULL) {
                Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);
                Console.WriteLine("Czas do końca: {0}", packet.Data);
            }
            if (packet.OP == OP_Enum.TIME && packet.OD == OD_Enum.TIME_OUT) {
                Console.WriteLine("ID: {0}, data: {1}, answer: {2}, operation: {3}", packet.ID, packet.Data, packet.OD, packet.OP);
                Console.WriteLine("GRA ZAKOŃCZONA, NIKT NIE ZGADNAL.");
                gameRunning = false;
                _udpClient.Close();
                Console.ReadLine();
            }
        }

        private static void InputNumber(Data.Packet packet) {
            bool isNumber;
            do {
                if (gameRunning == false) {
                    break;
                }
                int number;
                Console.WriteLine("Podaj liczbe:");
                var msg = Console.ReadLine();
                if (int.TryParse(msg, out number)) {
                    Data.Packet packetToSend = new Data.Packet(packet.ID, number, OD_Enum.NULL, OP_Enum.GUESS);
                    var stringToSend = packetToSend.Serialize();
                    byte[] bytesToSend = Encoding.UTF8.GetBytes(stringToSend);
                    _udpClient.Send(bytesToSend, bytesToSend.Length);
                    isNumber = true;
                } else {
                    Console.WriteLine("Nie podałeś liczby");
                    isNumber = false;
                }

            } while (!isNumber && gameRunning);
        }
    }
}
