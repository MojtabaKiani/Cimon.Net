using CimonPlc.Enums;
using System.Threading.Tasks;

namespace CimonPlc.Interfaces
{
    public interface IPlcSocket
    {
        public bool IsConnected { get; }

        Task<ConnectionStatus> Connect(int readTimeout = 1000, int writeTimeout = 1000, int pingTimeout = 3000);

        ConnectionStatus Disconnect();

        Task<bool> SendData(byte[] frame);

        Task<byte[]> ReceiveData();
    }
}
