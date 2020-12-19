using Ardalis.GuardClauses;
using CimonPlc.Enums;
using CimonPlc.Interfaces;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CimonPlc.Sockets
{
    public class SerialSocket : ISerialSocket
    {
        private readonly SerialPort _socket;

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
            return Task.FromResult(_socket.IsOpen ? ConnectionStatus.Connected : ConnectionStatus.DisConnected);
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

        public Task<byte[]> ReceiveData()
        {
            var frameLength = _socket.BytesToRead;
            var frame = new byte[frameLength];
            var result = _socket.Read(frame,0, frameLength);
            return result == frameLength ? Task.FromResult(frame) : null;
        }
    }
}
