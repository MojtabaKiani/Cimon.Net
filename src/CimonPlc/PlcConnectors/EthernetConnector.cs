using Ardalis.GuardClauses;
using CimonPlc.Interfaces;
using CimonPlc.Enums;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CimonPlc.Models;

namespace CimonPlc.PlcConnectors
{
    public class EthernetConnector : PlcConnector
    {
        private byte _frameNo = 0;
        public byte FrameNo => (byte)(_frameNo++ > 127 ? 0 : _frameNo);

        public EthernetConnector(IEthernetSocket socket, bool autoConnect = true) : base(socket)
        {
            _socket = socket;
            _autoConnect = autoConnect;
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to read according to
        ///     memory data type.The data can be assigned up to 16 pieces repeatedly.The
        ///     total sum of the word data should be not over 512 words.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to read such as X or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used. That is, in case of bit device such as X/Y, the last number should be '0' and should contains 6 character, such as '000010'</param>
        /// <param name="length">Requested length for reading memory. Length must be in the range from 1 to 512</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <returns>Returns a tuple includes PLC response code and an array of byte contains read data</returns>
        public override async Task<(ResponseCode responseCode, int[] data)> ReadWordAsync(MemoryType memoryType, string address, int length)
        {
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.OutOfRange(length, nameof(length), 1, 512);
            Guard.Against.BadFormat(address, nameof(address), @"[0-9a-fA-F]{0,5}0");


            while (address.Length < 6)
                address = "0" + address;

            if (!IsConnected && !_autoConnect)
                return (ResponseCode.SystemError, null);

            if (!IsConnected)
            {
                var connectionStatus = await Connect();
                if (connectionStatus != ConnectionStatus.Connected)
                    return (ResponseCode.SystemError, null);
            }

            var frame = new List<byte>();

            //[0-8] ID : This is a 9 - byte string  “ KDT_PLC_M ”.
            frame.AddRange(Encoding.ASCII.GetBytes("KDT_PLC_M"));

            //[9] Frame No : This, 1 - byte data with the range from 0 to 127
            frame.Add(FrameNo);

            //[10] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((byte)ReadCommand.WordBlockRead);

            //[11] Res: Reserved. (1 Byte, 00h) – Reserved device
            frame.Add(0);

            //[12] Length : Total number of the bytes of a frame data device
            frame.Add(10);

            //[13-n] Data : This is n block based on data needs to write and contains 2 parts :
            //      1- [13-20] Memory Address : 8 bytes
            frame.Add((byte)memoryType);
            frame.Add(0);
            frame.AddRange(Encoding.ASCII.GetBytes(address));

            //      2- [21-22] Read Length : 2 bytes
            frame.AddRange(length.ToDualByte());

            //Check Sum : This is 2-byte value. After the entire frame is binary-summed by the byte,
            //the lower 2 - byte in the result value is used.
            frame.AddRange(frame.Sum(x => x).ToDualByte());

            try
            {
                var SendState = await _socket.SendData(frame.ToArray());
                if (!SendState)
                    return (ResponseCode.WritingError, null);

                //Read response from PLC
                await Task.Delay(_timeout);
                var Recieveframe = await _socket.RecieveData();
                if (Recieveframe == null)
                    return (ResponseCode.SystemError, null);

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to 0x41, then must return error code
                if (Recieveframe[10] == 0x41)
                    return ((ResponseCode)Tools.ToInt(Recieveframe[14], Recieveframe[15]), null);

                if (!Tools.IsValidResponse(Recieveframe, frame[9], (byte)ReadCommand.WordBlockRead))
                    return (ResponseCode.WritingError, null);

                var tempArray = new List<int>();
                const int dataStartIndex = 23;
                for (var x = dataStartIndex; x < Recieveframe.Length - 2; x += 2)
                {
                    tempArray.Add(Tools.ToInt(Recieveframe[x], Recieveframe[x + 1]));
                }
                return (ResponseCode.Success, tempArray.ToArray());
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to read bit block. The data
        ///     can be assigned up to 16 pieces repeatedly.
        ///     But, The total sum of the word data is not to be over 1024 bits.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to read such as X or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used, it should contains 6 characters, such as '0000A1'</param>
        /// <param name="length">Requested length for reading memory. Length must be in the range from 1 to 1024</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <returns>Returns a tuple includes PLC response code and an array of byte contains read data</returns>
        public override async Task<(ResponseCode responseCode, byte[] data)> ReadBitAsync(MemoryType memoryType, string address, int length)
        {
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.OutOfRange(length, nameof(length), 1, 1024);
            Guard.Against.BadFormat(address, nameof(address), "[0-9a-fA-F]{1,6}");


            while (address.Length < 6)
                address = "0" + address;

            if (!IsConnected && !_autoConnect)
                return (ResponseCode.SystemError, null);

            if (!IsConnected)
            {
                var connectionStatus = await Connect();
                if (connectionStatus != ConnectionStatus.Connected)
                    return (ResponseCode.SystemError, null);
            }

            var frame = new List<byte>();

            //[0-8] ID : This is a 9 - byte string  “ KDT_PLC_M ”.
            frame.AddRange(Encoding.ASCII.GetBytes("KDT_PLC_M"));

            //[9] Frame No : This, 1 - byte data with the range from 0 to 127
            frame.Add(FrameNo);

            //[10] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((byte)ReadCommand.BitBlockRead);

            //[11] Res: Reserved. (1 Byte, 00h) – Reserved device
            frame.Add(0);

            //[12] Length : Total number of the bytes of a frame data device
            frame.Add(10);

            //[13-n] Data : This is n block based on data needs to write and contains 2 parts :
            //      1- [13-20] Memory Address : 8 bytes
            frame.Add((byte)memoryType);
            frame.Add(0);
            frame.AddRange(Encoding.ASCII.GetBytes(address));

            //      2- [21-22] Read Length : 2 bytes
            frame.AddRange(length.ToDualByte());

            //Check Sum : This is 2-byte value. After the entire frame is binary-summed by the byte,
            //the lower 2 - byte in the result value is used.
            frame.AddRange(frame.Sum(x => x).ToDualByte());

            try
            {
                var sendState = await _socket.SendData(frame.ToArray());
                if (!sendState)
                    return (ResponseCode.WritingError, null);

                //Read response from PLC
                await Task.Delay(_timeout);
                var recieveframe = await _socket.RecieveData();
                if (recieveframe == null)
                    return (ResponseCode.SystemError, null);

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to 0x41, then must return error code
                if (recieveframe[10] == 0x41)
                    return ((ResponseCode)Tools.ToInt(recieveframe[14], recieveframe[15]), null);

                if (!Tools.IsValidResponse(recieveframe, frame[9], (byte)ReadCommand.BitBlockRead))
                    return (ResponseCode.WritingError, null);

                //Data includes : Address 9 chars + data
                var dataLength = Tools.ToInt(recieveframe[12], recieveframe[13]);
                const int addressLength = 9;
                var tempArray = new byte[dataLength - addressLength];

                const int dataStartIndex = 23;
                Array.Copy(recieveframe, dataStartIndex, tempArray, 0, tempArray.Length);
                return (ResponseCode.Success, tempArray);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to write according to
        ///     memory data type.The data can be assigned up to 16 pieces repeatedly.
        ///     But, The total sum of word data is not to be over 64 words.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to write such as Y or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used. That is, in case of bit device such as X/Y, the last number should be '0' and should contains 6 characters, such as '000010'</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <param name="data">Data needed to write on PLC memory, The total sum of word data is not to be over 64 words</param>
        /// <returns>Returns a code which shows PLC response code</returns>
        public override async Task<ResponseCode> WriteWordAsync(MemoryType memoryType, string address, params int[] data)
        {
            Guard.Against.NullOrEmpty(data, nameof(data));
            Guard.Against.OutOfRange(data, nameof(data), 0, 0xFFFF);
            Guard.Against.OutOfRange(data.Length, nameof(data), 1, 64);
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.BadFormat(address, nameof(address), "[0-9a-fA-F]{0,5}0");

            while (address.Length < 6)
                address = "0" + address;

            if (!IsConnected && !_autoConnect)
                return ResponseCode.SystemError;

            if (!IsConnected)
            {
                var connectionStatus = await Connect();
                if (connectionStatus != ConnectionStatus.Connected)
                    return ResponseCode.SystemError;
            }

            var frame = new List<byte>();

            //[0-8] ID : This is a 9 - byte string  “ KDT_PLC_M ”.
            frame.AddRange(Encoding.ASCII.GetBytes("KDT_PLC_M"));

            //[9] Frame No : This, 1 - byte data with the range from 0 to 127
            frame.Add(FrameNo);

            //[10] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((byte)WriteCommands.WordBlockWrite);

            //[11] Res: Reserved. (1 Byte, 00h) – Reserved device
            frame.Add(0);

            //[12-13] Length : This is the 2 - byte value indicating the size of ‘Data’ field. (Hexadecimal Figure)
            int dataLength = data.Length * 2 + 10;
            frame.AddRange(dataLength.ToDualByte());

            //[14-n] Data : This is n block based on data needs to write and contains 3 parts :
            //      1- [14-21] Memory Address : 8 bytes
            frame.Add((byte)memoryType);
            frame.Add(0);
            frame.AddRange(Encoding.ASCII.GetBytes(address));

            //      2- [22-23] Write Size : 2 bytes
            frame.AddRange(data.Length.ToDualByte());

            //      3- [24-n] Word Data : n bytes
            foreach (var item in data)
            {
                frame.AddRange(item.ToDualByte());
            }

            //Check Sum : This is 2-byte value. After the entire frame is binary-summed by the byte,
            //the lower 2 - byte in the result value is used.
            frame.AddRange(frame.Sum(x => x).ToDualByte());

            try
            {
                var SendState = await _socket.SendData(frame.ToArray());
                if (!SendState)
                    return ResponseCode.WritingError;

                //Read response from PLC
                await Task.Delay(_timeout);
                var Recieveframe = await _socket.RecieveData();
                if (Recieveframe == null)
                    return ResponseCode.SystemError;

                if (_autoConnect)
                    _socket.Disconnect();

                if (!Tools.IsValidResponse(Recieveframe, frame[9], 0x41))
                    return ResponseCode.WritingError;

                return (ResponseCode)Tools.ToInt(Recieveframe[14], Recieveframe[15]);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to write according to
        ///     memory data type.The data can be assigned up to 16 pieces repeatedly.
        ///     But,  The total sum of the data is not to be over 256 bits.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to write such as Y or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used, it should contains 6 characters, such as '0000D1'</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <param name="data">Data needed to write on PLC memory, The total sum of word data is not to be over 256 bits</param>
        /// <returns>Returns a code which shows PLC response co`de</returns>
        public override async Task<ResponseCode> WriteBitAsync(MemoryType memoryType, string address, params byte[] data)
        {
            Guard.Against.NullOrEmpty(data, nameof(data));
            Guard.Against.OutOfRange<byte>(data, nameof(data), 0, 0xFF);
            Guard.Against.OutOfRange(data.Length, nameof(data), 1, 256);
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.BadFormat(address, nameof(address), "[0-9a-fA-F]{1,6}");

            while (address.Length < 6)
                address = "0" + address;

            if (!IsConnected && !_autoConnect)
                return ResponseCode.SystemError;

            if (!IsConnected)
            {
                var connectionStatus = await Connect();
                if (connectionStatus != ConnectionStatus.Connected)
                    return ResponseCode.SystemError;
            }

            var frame = new List<byte>();

            //[0-8] ID : This is a 9 - byte string  “ KDT_PLC_M ”.
            frame.AddRange(Encoding.ASCII.GetBytes("KDT_PLC_M"));

            //[9] Frame No : This, 1 - byte data with the range from 0 to 127
            frame.Add(FrameNo);

            //[10] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((byte)WriteCommands.BitBlockWrite);

            //[11] Res: Reserved. (1 Byte, 00h) – Reserved device
            frame.Add(0);

            //[12-13] Length : This is the 2 - byte value indicating the size of ‘Data’ field. (Hexadecimal Figure)
            int dataLength = data.Length + 10;
            frame.AddRange(dataLength.ToDualByte());

            //[14-n] Data : This is n block based on data needs to write and contains 3 parts :
            //      1- [14-21] Memory Address : 8 bytes
            frame.Add((byte)memoryType);
            frame.Add(0);
            frame.AddRange(Encoding.ASCII.GetBytes(address));

            //      2- [22-23] Write Size : 2 bytes
            frame.AddRange(data.Length.ToDualByte());

            //      3- [24-n] Word Data : n bytes
            frame.AddRange(data);

            //Check Sum : This is 2-byte value. After the entire frame is binary-summed by the byte,
            //the lower 2 - byte in the result value is used.
            frame.AddRange(frame.Sum(x => x).ToDualByte());

            try
            {
                var SendState = await _socket.SendData(frame.ToArray());
                if (!SendState)
                    return ResponseCode.WritingError;

                //Read response from PLC
                await Task.Delay(_timeout);
                var Recieveframe = await _socket.RecieveData();
                if (Recieveframe == null)
                    return ResponseCode.SystemError;

                if (_autoConnect)
                    _socket.Disconnect();

                if (!Tools.IsValidResponse(Recieveframe, frame[9], 0x41))
                    return ResponseCode.WritingError;

                return (ResponseCode)Tools.ToInt(Recieveframe[14], Recieveframe[15]);

            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
