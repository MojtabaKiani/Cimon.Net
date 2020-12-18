using System;
using Xunit;
using CimonPlc.PlcConnectors;
using CimonPlc.Enums;
using CimonPlc.UnitTests.FakeCalsses;

namespace CimonPlc.UnitTests
{
    public class SerialConnectorTests
    {
        private readonly SerialConnector _connector;

        public SerialConnectorTests()
        {
            _connector = new SerialConnector(new FakeSerialSocket());
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
        [InlineData(MemoryType.Y, "00010", 60)]
        [InlineData(MemoryType.D, "000F0", 6)]
        [InlineData(MemoryType.M, "0F010", 50)]
        [InlineData(MemoryType.L, "000F10", 30)]
        public async void ReadWordAsync_Should_Return_Value_On_Correct_Data(MemoryType memoryType, string address, int length)
        {
            var (responseCode, data) = await _connector.ReadWordAsync(memoryType, address, length);
            Assert.Equal(ResponseCode.Success, responseCode);
            Assert.Equal(length * 2, data.Length);
        }

        [Theory]
        [InlineData(MemoryType.X, "00FF0F1", 10)]
        [InlineData(MemoryType.D, "000x0", 6)]
        [InlineData(MemoryType.D, "000051", 6)]
        [InlineData(MemoryType.M, "0F01", 65)]
        [InlineData(MemoryType.Y, "000F1", 0)]
        public async void ReadWordAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, int length)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.ReadWordAsync(memoryType, address, length));
        }


        [Theory]
        [InlineData(MemoryType.X, "000F1", 10)]
        [InlineData(MemoryType.Y, "0", 52)]
        [InlineData(MemoryType.D, "000F5", 6)]
        [InlineData(MemoryType.M, "0F01", 126)]
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
        [InlineData(MemoryType.M, "0F01", 256)]
        [InlineData(MemoryType.Y, "000F1", 0)]
        public async void ReadBitAsync_Should_Return_Error_On_Incorrect_Data(MemoryType memoryType, string address, int length)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => _connector.ReadBitAsync(memoryType, address, length));
        }
    }
}
