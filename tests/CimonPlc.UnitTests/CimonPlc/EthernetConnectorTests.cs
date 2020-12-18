using System;
using Xunit;
using CimonPlc.PlcConnectors;
using CimonPlc.Enums;
using CimonPlc.UnitTests.FakeCalsses;

namespace CimonPlc.UnitTests
{
    public class EthernetConnectorTests
    {
        private readonly EthernetConnector _connector;

        public EthernetConnectorTests()
        {
            _connector = new EthernetConnector(new FakeSocket());
        }

        [Theory]
        [InlineData(100, 100, 100)]
        [InlineData(1000, 1000, 500)]
        [InlineData(1000, 1000, 1000)]
        [InlineData(5000, 4000, 3000)]
        [InlineData(1000, 1000, 5000)]
        public async void Connect_Should_Return_Connected_On_Valid_Data(int readTimeout, int writeTimeout, int pingTimeout)
        {
            var result = await _connector.Connect(readTimeout, writeTimeout, pingTimeout);
            Assert.Equal(ConnectionStatus.Connected, result);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(-1000, -1000, -500)]
        [InlineData(0, 1000, 1000)]
        [InlineData(1000, 0, 1000)]
        [InlineData(1000, 1000, 0)]
        public async void Connect_Should_Return_Error_On_Incorrect_Data(int readTimeout, int writeTimeout, int pingTimeout)
        {
            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => _connector.Connect(readTimeout, writeTimeout, pingTimeout));
        }

        [Theory]
        [InlineData(MemoryType.X, "000F0", 10)]
        [InlineData(MemoryType.Y, "00010", 512)]
        [InlineData(MemoryType.D, "000F0", 6)]
        [InlineData(MemoryType.M, "0F010", 100)]
        [InlineData(MemoryType.L, "000F10", 100)]
        public async void ReadWordAsync_Should_Return_Value_On_Correct_Data(MemoryType memoryType, string address, int length)
        {
            var (responseCode, data) = await _connector.ReadWordAsync(memoryType, address, length);
            Assert.Equal(ResponseCode.Success, responseCode);
            Assert.Equal(length, data.Length);
        }

        [Theory]
        [InlineData(MemoryType.X, "00FF0F1", 10)]
        [InlineData(MemoryType.D, "000x0", 6)]
        [InlineData(MemoryType.D, "000051", 6)]
        [InlineData(MemoryType.M, "0F01", 600)]
        [InlineData(MemoryType.Y, "000F1", 0)]
        public async void ReadWordAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, int length)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.ReadWordAsync(memoryType, address, length));
        }

        [Theory]
        [InlineData(MemoryType.X, "000F1", 10)]
        [InlineData(MemoryType.Y, "0", 1024)]
        [InlineData(MemoryType.D, "000F5", 6)]
        [InlineData(MemoryType.M, "0F01", 100)]
        [InlineData(MemoryType.L, "000F1", 100)]
        public async void ReadBitAsync_Should_Return_Value_On_Correct_Data(MemoryType memoryType, string address, int length)
        {
            var (responseCode, data) = await _connector.ReadBitAsync(memoryType, address, length);
            Assert.Equal(ResponseCode.Success, responseCode);
            Assert.Equal(length, data.Length);
        }

        [Theory]
        [InlineData(MemoryType.X, "00FF0F1", 10)]
        [InlineData(MemoryType.D, "000x5", 6)]
        [InlineData(MemoryType.M, "0F01", 1060)]
        [InlineData(MemoryType.Y, "000F1", 0)]
        public async void ReadBitAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, int length)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.ReadBitAsync(memoryType, address, length));
        }

        [Theory]
        [InlineData(MemoryType.X, "000F0", 10, 100, 1000)]
        [InlineData(MemoryType.Y, "0", 1024, 35000)]
        [InlineData(MemoryType.D, "000F0", 16050)]
        [InlineData(MemoryType.M, "0F010", 100, 100, 10000, 1200, 1400)]
        [InlineData(MemoryType.L, "000F0", 1010, 65000, 3403, 2302)]
        public async void WriteWordAsync_Should_Return_Value_On_Correct_Data(MemoryType memoryType, string address, params int[] data)
        {
            var result = await _connector.WriteWordAsync(memoryType, address, data);
            Assert.Equal(ResponseCode.Success, result);
        }

        [Theory]
        [InlineData(MemoryType.X, "00FF01", 10, 110, 0, 1205)]
        [InlineData(MemoryType.D, "000x5", 6)]
        [InlineData(MemoryType.M, "0F00", 70000, 30000, 40000)]
        [InlineData(MemoryType.Y, "000F0", -10, 100, 1000)]
        [InlineData(MemoryType.Y, "0")]
        public async void WriteWordAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, params int[] data)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.WriteWordAsync(memoryType, address, data));
        }


        [Theory]
        [InlineData(MemoryType.X, "000F0", (byte)1, (byte)1, (byte)1)]
        [InlineData(MemoryType.Y, "0", (byte)1, (byte)1, (byte)1, (byte)1)]
        [InlineData(MemoryType.D, "000F1", (byte)1, (byte)1)]
        [InlineData(MemoryType.M, "0F011", (byte)1, (byte)1, (byte)0, (byte)1, (byte)1)]
        [InlineData(MemoryType.L, "000F0", (byte)1, (byte)1, (byte)1, (byte)0)]
        public async void WriteBitAsync_Should_Return_Value_On_Correct_Data(MemoryType memoryType, string address, params byte[] data)
        {
            var result = await _connector.WriteBitAsync(memoryType, address, data);
            Assert.Equal(ResponseCode.Success, result);
        }

        [Theory]
        [InlineData(MemoryType.X, "00FF001", (byte)1, (byte)1, (byte)0, (byte)1)]
        [InlineData(MemoryType.D, "000x5", (byte)1)]
        [InlineData(MemoryType.D, "000F5", (byte)2)]
        [InlineData(MemoryType.Y, "0")]
        public async void WriteBitAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, params byte[] data)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.WriteBitAsync(memoryType, address, data));
        }
    }
}
