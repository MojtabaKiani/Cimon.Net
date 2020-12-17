using Ardalis.GuardClauses;
using CimonPlc.Enums;
using CimonPlc.Interfaces;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CimonPlc.Sockets
{
    public class SerialSocket : IPlcSocket
    {
        private readonly SerialPort _socket;
        private readonly string _portName;
        private readonly int _baudRate;

        public bool IsConnected => _socket.IsOpen;



        public SerialSocket(string portName, int baudRate = 9600)
        {
            Guard.Against.NullOrEmpty(portName, nameof(portName));
            Guard.Against.OutOfRange(baudRate, nameof(baudRate), 75, 256000);

            _socket = new SerialPort(portName, baudRate);
        }

        public Task<ConnectionStatus> Connect(int readTimeout = 1000, int writeTimeout = 1000, int pingTimeout = 3000)
        {
            _socket.ReadTimeout = readTimeout;
            _socket.WriteTimeout = writeTimeout;

            _socket.Open();
            if (_socket.IsOpen)
                return Task.FromResult(ConnectionStatus.Connected);

            return Task.FromResult(ConnectionStatus.DisConnected);
        }

        public ConnectionStatus Disconnect()
        {
            _socket.Close();
            return ConnectionStatus.DisConnected;
        }

        public Task<bool> SendData(byte[] frame)
        {
            _socket.Write(frame,0,frame.Length);
            return Task.FromResult(true);
        }

        public Task<byte[]> RecieveData()
        {
            var frameLength = _socket.BytesToRead;
            var frame = new byte[frameLength];
            var result = _socket.Read(frame,0, frameLength);
            if (result == frameLength)
                return Task.FromResult(frame);

            return null;
        }
    }
}
