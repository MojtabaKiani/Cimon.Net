using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace CimonPlc
{
    public static class Tools
    {
        public static IPStatus Ping(IPAddress ip,int pingTimeout=300)
        {
            try
            {
                var pingSender = new Ping();
                var reply = pingSender.Send(ip, pingTimeout);
                return reply.Status;
            }
            catch
            {
                return IPStatus.BadDestination;
            }
        }

        public static int ToInt(byte byte1, byte byte2)
        {
            return (byte1 << 8) + byte2;
        }

        public static int ToInt(char char1, char char2)
        {
          return  int.Parse(string.Concat(char1, char2), System.Globalization.NumberStyles.HexNumber);
        }
        
        public static byte[] ToDualByte(this int number)
        {
            number &= 0xFFFF;
            return new byte[] { (byte)(number >> 8), (byte)(number & 0xFF) };
        }

        public static char[] ToDualChar(this int number)
        {
            number &= 0xFFFF;
            return number.ToString("X2").ToArray();
        }

        public static void AddBCC(this List<char> input)
        {
            if (input == null || input.Count == 0)
                return;

            // Exclude 3 start chars during BCC calculation
            var sum = input.Skip(3).Sum(x => Convert.ToByte(x));
            sum %= 256;

            input.AddRange(sum.ToDualChar());
        }

        public static bool IsValidResponse(byte[] buffer, byte frameNo, int ackCommand)
        {
            //[0-8] ID : This is a 9 - byte string  “ KDT_PLC_S ”.
            if (Encoding.UTF8.GetString(buffer, 0, 9) != "KDT_PLC_S")
                return false;

            //[9] Frame No : This is 1 - byte data.The value that 128 are added to the number of the
            //      command frame received from the Master is used
            if (buffer[9] - 128 != frameNo)
                return false;

            //[10] Cmd :In Slave, 1 - byte command as specified in parameter can be used and the
            //          format of ‘Data’ field is selected according to command
            if (buffer[10] != ackCommand)
                return false;

            //Check Sum : This is 2-byte value. After the entire frame is binary-summed by the byte,
            //the lower 2 - byte in the result value is used.
            var tempArray = new byte[buffer.Length - 2];
            Array.Copy(buffer, 0, tempArray, 0, tempArray.Length);
            if (!tempArray.Sum(x => x).ToDualByte().SequenceEqual(new byte[] { buffer[^2], buffer[^1] }))
                return false;

            return true;

        }

        internal static bool IsValidSerialResponse(char[] recieveframe, byte ackCommand)
        {
            if (recieveframe[0]!=(char)2 || recieveframe[^1] != (char)3)
                return false;

            //[3] Cmd :In Slave, 1 - byte command must be equal to sended command
            if (recieveframe[3] != (char)ackCommand)
                return false;

            return true;
        }
    }
}
