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
    public class SerialConnector : PlcConnector
    {
        public SerialConnector(ISerialSocket socket, bool autoConnect = true) : base(socket)
        {
            _socket = socket;
            _autoConnect = autoConnect;
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to read according to
        ///     memory data type.The total sum of the word data should be not over 63 words.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to read. Valid symbols are X, Y, M, L, K, F, Z, TC, TS, CC, CS, D, S</param>
        /// <param name="address">The word address or the card number of a corresponding device is used. That is, in case of bit device such as X/Y, the last number should be '0' and should contains 6 character, such as '000010'</param>
        /// <param name="length">Requested length for reading memory. Length must be in the range from 1 to 63</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <returns>Returns a tuple includes PLC response code and an array of byte contains read data</returns>
        public override async Task<(ResponseCode responseCode, int[] data)> ReadWordAsync(MemoryType memoryType, string address, int length)
        {
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.OutOfRange(length, nameof(length), 1, 63);
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

            var frame = new List<char>();

            //[0] HEADER : This is 1-byte control letter of ASCII code “ENQ”.
            frame.Add((char)0x5);

            //[1-2] PLC Station Number : This, 2 - byte data with the range from 0 to 127, if station numbers are assigned, multi-drop communication is available
            frame.Add('0');
            frame.Add('0');

            //[3] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((char)ReadCommand.WordBlockRead);

            //[4-5] Length : Total number of the chars of a frame data device
            // It contains 8 chars for address + 2 chars for length
            frame.Add('0');
            frame.Add('A');

            //[6-15] Data : This is n block based on data needs to write and contains 2 parts :
            //      1- [6-13] Memory Address : 8 chars
            frame.Add(memoryType.ToString()[0]);
            frame.Add('0');
            frame.AddRange(address.ToCharArray());

            //      2- [14-15] Read Length : 2 chars
            frame.AddRange(((byte)length).ToDualChar());

            //[16-17] BCC : is the remainder value when dividing the binary-sum from Cmd to the end of data by 256.
            frame.AddBCC();

            //[18] End : This is 1-byte control letter of ASCII code “EOT”.
            frame.Add((char)0x4);

            try
            {
                var SendState = await _socket.SendData(frame.Select(X=> Convert.ToByte(X)).ToArray());
                if (!SendState)
                    return (ResponseCode.WritingError, null);

                //Read response from PLC
                await Task.Delay(_timeout);
                var tempframe = await _socket.RecieveData();
                var recievedFrame = tempframe.Select(x => (char)x).ToArray();
                if (recievedFrame == null)
                    return (ResponseCode.SystemError, null);

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to E(0X45), then must return error code
                if (recievedFrame[3] == 'E')
                    return ((ResponseCode)Tools.ToByte(recievedFrame[6], recievedFrame[7]), null);
                    
                if (!Tools.IsValidSerialResponse(recievedFrame, (byte)ReadCommand.WordBlockRead))
                    return (ResponseCode.WritingError, null);

                var tempArray = new List<int>();
                const int dataStartIndex = 6;
                int dataLength = Tools.ToByte(recievedFrame[4], recievedFrame[5]);
                for (var x = dataStartIndex; x < dataLength + dataStartIndex; x += 4)
                {
                    var byte1 = Tools.ToByte(recievedFrame[x], recievedFrame[x + 1]);
                    var byte2 = Tools.ToByte(recievedFrame[x+2], recievedFrame[x + 3]);
                    tempArray.Add(Tools.ToInt(byte1, byte2));
                }
                return (ResponseCode.Success, tempArray.ToArray());
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to read bit block. 
        ///     The total sum of the data is not to be over 126 bits.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to read such as X or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used, it should contains 6 characters, such as '0000A1'</param>
        /// <param name="length">Requested length for reading memory. Length must be in the range from 1 to 126</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <returns>Returns a tuple includes PLC response code and an array of byte contains read data</returns>
        public override async Task<(ResponseCode responseCode, byte[] data)> ReadBitAsync(MemoryType memoryType, string address, int length)
        {
            Guard.Against.Null(memoryType, nameof(memoryType));
            Guard.Against.OutOfRange(length, nameof(length), 1, 126);
            Guard.Against.BadFormat(address, nameof(address), @"[0-9a-fA-F]{1,6}");


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

            var frame = new List<char>();

            //[0] HEADER : This is 1-byte control letter of ASCII code “ENQ”.
            frame.Add((char)0x5);

            //[1-2] PLC Station Number : This, 2 - byte data with the range from 0 to 127, if station numbers are assigned, multi-drop communication is available
            frame.Add('0');
            frame.Add('0');

            //[3] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((char)ReadCommand.BitBlockRead);

            //[4-5] Length : Total number of the chars of a frame data device
            // It contains 8 chars for address + 2 chars for length
            frame.Add('0');
            frame.Add('A');

            //[6-15] Data : This is n block based on data needs to write and contains 2 parts :
            //      1- [6-13] Memory Address : 8 chars
            frame.Add(memoryType.ToString()[0]);
            frame.Add('0');
            frame.AddRange(address.ToCharArray());

            //      2- [14-15] Read Length : 2 chars
            frame.AddRange(((byte)length).ToDualChar());

            //[16-17] BCC : is the remainder value when dividing the binary-sum from Cmd to the end of data by 256.
            frame.AddBCC();

            //[18] End : This is 1-byte control letter of ASCII code “EOT”.
            frame.Add((char)0x4);

            try
            {
                var SendState = await _socket.SendData(frame.Select(X => Convert.ToByte(X)).ToArray());
                if (!SendState)
                    return (ResponseCode.WritingError, null);

                //Read response from PLC
                await Task.Delay(_timeout);
                var tempframe = await _socket.RecieveData();
                var recievedFrame = tempframe.Select(x => (char)x).ToArray();
                if (recievedFrame == null)
                    return (ResponseCode.SystemError, null);

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to E(0X45), then must return error code
                if (recievedFrame[3] == 'E')
                    return ((ResponseCode)Tools.ToByte(recievedFrame[6], recievedFrame[7]), null);

                if (!Tools.IsValidSerialResponse(recievedFrame, (byte)ReadCommand.BitBlockRead))
                    return (ResponseCode.WritingError, null);

                var tempArray = new List<int>();
                const int dataStartIndex = 6;
                int dataLength = Tools.ToByte(recievedFrame[4], recievedFrame[5]);
                for (var x = dataStartIndex; x < dataLength + dataStartIndex; x += 2)
                {
                    tempArray.Add(Tools.ToByte(recievedFrame[x], recievedFrame[x + 1]));
                }
                return (ResponseCode.Success, tempArray.Select(x => (byte)x).ToArray());

            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to write according to
        ///     memory data type. The total sum of word data is not to be over 63 words.
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
            Guard.Against.OutOfRange(data.Length, nameof(data), 1, 63);
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

            var frame = new List<char>();

            //[0] HEADER : This is 1-byte control letter of ASCII code “ENQ”.
            frame.Add((char)0x5);

            //[1-2] PLC Station Number : This, 2 - byte data with the range from 0 to 127, if station numbers are assigned, multi-drop communication is available
            frame.Add('0');
            frame.Add('0');

            //[3] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((char)WriteCommands.WordBlockWrite);

            //[4-5] Length : Total number of the chars of a frame data device
            // It contains 8 chars for address + 2 chars word count + 4 chars for each word
            var length = 10 + data.Length * 4 ;
            frame.AddRange(((byte)length).ToDualChar());

            //[6-15] Data : This is n block based on data needs to write and contains 3 parts :
            //      1- [6-13] Memory Address : 8 chars
            frame.Add(memoryType.ToString()[0]);
            frame.Add('0');
            frame.AddRange(address.ToCharArray());

            //      2- [14-15] Read Length : 2 chars
            frame.AddRange(((byte)data.Length).ToDualChar());

            //      3- [15-n] Words to write : 4 chars per word
            foreach (var word in data)
                frame.AddRange(word.ToQuadChar());

            //[^3-^2] BCC : is the remainder value when dividing the binary-sum from Cmd to the end of data by 256.
            frame.AddBCC();

            //[^1] End : This is 1-byte control letter of ASCII code “EOT”.
            frame.Add((char)0x4);

            try
            {
                var SendState = await _socket.SendData(frame.Select(X => Convert.ToByte(X)).ToArray());
                if (!SendState)
                    return ResponseCode.WritingError;

                //Read response from PLC
                await Task.Delay(_timeout);
                var tempframe = await _socket.RecieveData();
                var recievedFrame = tempframe.Select(x => (char)x).ToArray();
                if (recievedFrame == null)
                    return ResponseCode.SystemError;

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to E(0X45), then must return error code
                if (recievedFrame[3] == 'E')
                    return (ResponseCode)Tools.ToByte(recievedFrame[6], recievedFrame[7]);

                if (!Tools.IsValidSerialResponse(recievedFrame, (byte)WriteCommands.WordBlockWrite))
                    return ResponseCode.WritingError;
               
                return (ResponseCode)Tools.ToByte(recievedFrame[4], recievedFrame[5]);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///     This function is to assign PLC device memory directly to write according to
        ///     memory data type.The total sum of the data is not to be over 126 bits.
        /// </summary>
        /// <param name="memoryType">PLC memory which required to write such as Y or D</param>
        /// <param name="address">The word address or the card number of a corresponding device is used, it should contains 6 characters, such as '0000D1'</param>
        /// <param name="autoConnect">It tries to connect to PLC if the connection state is disconnect</param>
        /// <param name="data">Data needed to write on PLC memory, The total sum of word data is not to be over 256 bits</param>
        /// <returns>Returns a code which shows PLC response co`de</returns>
        public override async Task<ResponseCode> WriteBitAsync(MemoryType memoryType, string address, params byte[] data)
        {
            Guard.Against.NullOrEmpty(data, nameof(data));
            Guard.Against.OutOfRange<byte>(data, nameof(data), 0, 1);
            Guard.Against.OutOfRange(data.Length, nameof(data), 1, 126);
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

            var frame = new List<char>();

            //[0] HEADER : This is 1-byte control letter of ASCII code “ENQ”.
            frame.Add((char)0x5);

            //[1-2] PLC Station Number : This, 2 - byte data with the range from 0 to 127, if station numbers are assigned, multi-drop communication is available
            frame.Add('0');
            frame.Add('0');

            //[3] Cmd : In Master, 1 - byte command can be used and the
            //format of ‘Data’ field is selected according to each command
            frame.Add((char)WriteCommands.BitBlockWrite);

            //[4-5] Length : Total number of the chars of a frame data device
            // It contains 8 chars for address + 2 chars word count + 1 char per bit
            var length = 10 + data.Length ;
            frame.AddRange(((byte)length).ToDualChar());

            //[6-15] Data : This is n block based on data needs to write and contains 3 parts :
            //      1- [6-13] Memory Address : 8 chars
            frame.Add(memoryType.ToString()[0]);
            frame.Add('0');
            frame.AddRange(address.ToCharArray());

            //      2- [14-15] Read Length : 2 chars
            frame.AddRange(((byte)data.Length).ToDualChar());

            //      3- [15-n] Words to write : 1 char per bit
            foreach (var bit in data)
                frame.Add((char)bit);

            // [^3-^2]BCC : 2 chars is the remainder value when dividing the binary-sum from Cmd to the end of data by 256.
            frame.AddBCC();

            //[^1] End : This is 1-byte control letter of ASCII code “EOT”.
            frame.Add((char)0x4);

            try
            {
                var SendState = await _socket.SendData(frame.Select(X => Convert.ToByte(X)).ToArray());
                if (!SendState)
                    return ResponseCode.WritingError;

                //Read response from PLC
                await Task.Delay(_timeout);
                var tempframe = await _socket.RecieveData();
                var recievedFrame = tempframe.Select(x => (char)x).ToArray();
                if (recievedFrame == null)
                    return ResponseCode.SystemError;

                if (_autoConnect)
                    _socket.Disconnect();

                //If response's cmd equals to E(0X45), then must return error code
                if (recievedFrame[3] == 'E')
                    return (ResponseCode)Tools.ToByte(recievedFrame[6], recievedFrame[7]);

                if (!Tools.IsValidSerialResponse(recievedFrame, (byte)WriteCommands.BitBlockWrite))
                    return ResponseCode.WritingError;

                return (ResponseCode)Tools.ToByte(recievedFrame[4], recievedFrame[5]);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
