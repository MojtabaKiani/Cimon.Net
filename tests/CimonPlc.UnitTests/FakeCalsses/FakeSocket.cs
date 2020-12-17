using CimonPlc.Enums;
using CimonPlc.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CimonPlc.UnitTests.FakeCalsses
{
    public class FakeSocket : IPlcSocket
    {
        public bool IsConnected => true;

        public byte FrameNo { get; set; }
        public byte Command { get; private set; }
        public int Length { get; private set; }

        public async Task<ConnectionStatus> Connect(int readTimeout = 1000, int writeTimeout = 1000, int pingTimeout = 3000)
        {
            await Task.Delay(100);
            return ConnectionStatus.Connected;
        }

        public ConnectionStatus Disconnect()
        {
            return ConnectionStatus.DisConnected;
        }

        public async Task<byte[]> RecieveData()
        {
            await Task.Delay(100);
            var frame = new List<byte>();
            frame.AddRange(Encoding.ASCII.GetBytes("KDT_PLC_S"));
            frame.Add((byte)(FrameNo + 128));
            frame.Add(Command);
            frame.Add(0);

            switch (Command)
            {
                case (byte)WriteCommands.BitBlockWrite:
                case (byte)WriteCommands.WordBlockWrite:
                    frame[10] = 0x41;
                    frame.AddRange(new byte[] { 0, 2, 0, 0 });
                    break;
                case (byte)ReadCommand.BitBlockRead:
                    frame.AddRange((9 + Length).ToDualByte());
                    frame.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                    for (var i = 0; i < Length; i++)
                        frame.Add(1);
                    break;
                case (byte)ReadCommand.WordBlockRead:
                    frame.AddRange((9 + Length * 2).ToDualByte());
                    frame.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                    for (var i = 0; i < Length; i++)
                        frame.AddRange(new byte[] { 1, 1 });
                    break;
            }
            frame.AddRange(frame.Sum(x => x).ToDualByte());

            return frame.ToArray();
        }

        public async Task<bool> SendData(byte[] frame)
        {
            await Task.Delay(100);
            FrameNo = frame[9];
            Command = frame[10];
            Length = Tools.ToInt(frame[21], frame[22]);
            return true;
        }
    }
}
