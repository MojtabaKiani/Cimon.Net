using Ardalis.GuardClauses;
using CimonPlc.Enums;
using CimonPlc.Interfaces;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CimonPlc.Sockets
{
    public class TcpSocket : IEthernetSocket
    {
        private readonly Socket _socket;
        private readonly IPAddress _ip;
        private readonly int _port;

        public bool IsConnected => _socket.Connected;

        public TcpSocket(string ip, int port = 10620)
        {
            Guard.Against.OutOfRange(port, nameof(port), 1, 65535);
            Guard.Against.NullOrEmpty(ip, nameof(ip));
            Guard.Against.InvalidData(ip, nameof(ip), x => IPAddress.TryParse(x, out IPAddress tempIp));

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ip = IPAddress.Parse(ip);
            _port = port;
        }

        public async Task<ConnectionStatus> Connect(int readTimeout = 1000, int writeTimeout = 1000, int pingTimeout = 3000)
        {
            if (Tools.Ping(_ip, pingTimeout) != IPStatus.Success)
            {
                return ConnectionStatus.NoRouteToDestination;
            }

            var remoteEp = new IPEndPoint(_ip, _port);
            _socket.ReceiveTimeout = readTimeout;
            _socket.SendTimeout = writeTimeout;

            await _socket.ConnectAsync(remoteEp);
            return _socket.Connected ? ConnectionStatus.Connected : ConnectionStatus.DisConnected;
        }

        public ConnectionStatus Disconnect()
        {
            _socket.Close();
            return ConnectionStatus.DisConnected;
        }

        public async Task<bool> SendData(byte[] frame)
        {
           var result= await _socket.SendAsync(frame, SocketFlags.None);
            return (result == frame.Length);
        }

        public async Task<byte[]> ReceiveData()
        {
            var frameLength = _socket.Available;
            var frame = new byte[frameLength];
            var result = await _socket.ReceiveAsync(frame, SocketFlags.None);
            return result == frameLength ? frame : null;
        }
    }
}
