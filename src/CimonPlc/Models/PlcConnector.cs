using Ardalis.GuardClauses;
using CimonPlc.Enums;
using CimonPlc.Interfaces;
using System;
using System.Threading.Tasks;

namespace CimonPlc.Models
{
    public abstract class PlcConnector
    {
        protected IPlcSocket _socket;
        protected bool _autoConnect;
        protected int _timeout;

        public bool IsConnected => _socket.IsConnected;

        public PlcConnector(IPlcSocket socket)
        {
            _socket = socket;
        }

        /// <summary>
        /// Creates a connection to PLC using a network socket, before read or write data it must be called.
        /// </summary>
        /// <param name="readTimeout">Data read timeout in ms, valid rage is between 100 and 10,000</param>
        /// <param name="writeTimeout">Data write timeout in ms, valid rage is between 100 and 10,000</param>
        /// <param name="pingTimeout">Ping Timeout in ms, valid rage is between 100 and 10,000</param>
        /// <returns>Returns success if it can connect to PLC successfully</returns>
        public virtual async Task<ConnectionStatus> Connect(int readTimeout = 1000, int writeTimeout = 1000, int pingTimeout = 3000)
        {
            Guard.Against.OutOfRange(readTimeout, nameof(readTimeout), 100, 10000);
            Guard.Against.OutOfRange(writeTimeout, nameof(writeTimeout), 100, 10000);
            Guard.Against.OutOfRange(pingTimeout, nameof(pingTimeout), 100, 10000);

            try
            {
                _timeout = readTimeout;
                return await _socket.Connect(readTimeout, writeTimeout, pingTimeout);
            }
            catch (Exception)
            {
                throw;
            }

        }

        /// <summary>
        /// Drops the connection to PLC, it should call if you don't set auto connection in read/write functions.
        /// </summary>
        /// <returns>Returns success if it can disconnect from PLC successfully</returns>
        public virtual ConnectionStatus Disconnect()
        {
            try
            {
                return _socket.Disconnect();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public abstract Task<(ResponseCode responseCode, int[] data)> ReadWordAsync(MemoryType memoryType, string address, int length);

        public abstract Task<(ResponseCode responseCode, byte[] data)> ReadBitAsync(MemoryType memoryType, string address, int length);

        public abstract Task<ResponseCode> WriteWordAsync(MemoryType memoryType, string address, params int[] data);

        public abstract Task<ResponseCode> WriteBitAsync(MemoryType memoryType, string address, params byte[] data);

    }
}
