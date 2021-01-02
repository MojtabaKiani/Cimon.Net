[![NuGet version](https://badge.fury.io/nu/Cimon.Net.svg)](https://badge.fury.io/nu/Cimon.Net)

# Cimon.Net
A .NET Library for Cimon PLCs Connectivity

<!-- ABOUT THE PROJECT -->
## About The Project
<p>CIMON-PLC is an industrial control device based on international standards of IEC61131. There are many tools and servers to work with PLCs and send/recieve data with them, but what if we want to use our application to communicate? I Couldn't find any library to work with Cimon PLCs, and actually I used them in many projects. So I decided to publish my library.</p>

## Getting Started
### 1. Installing Cimon.Net
You can install Cimon.Net with [NuGet Package Manager Console](https://www.nuget.org/packages/Cimon.Net):

    Install-Package Cimon.Net
    
Or via the .NET Core command-line interface:

    dotnet add package Cimon.Net
    
Either commands, from Package Manager Console or .NET Core CLI, will download and install **Cimon.Net** and all required dependencies.

### 2. Defining your Connector
Create a Connector based on your connection type, You can choose between `EthernetConnector` to support Ethernet TCP/UDP connection or `SerialConnector` to support RS232C/RS485 serial interfaces.

`EthernetConnector` usage sample for reading 10 bits from Input device memory `X` address 000001:
   
    var Plc = new EthernetConnector(new TcpSocket("192.168.1.10", 10620), true);
    var (responseCode, data) = await Plc.ReadBitAsync(MemoryType.X, "000001", 10);

`SerialConnector` usage sample for write 5 bits to Output device memory `Y` address 000010:
   
    var Plc = new SerialConnector(new SerialSocket("COM3", 9600), true);
    await Plc.WriteBitAsync(MemoryType.Y, "000010", 1, 1, 1, 0, 1);
    
## Documentation
Check the Wiki and feel free to edit it: https://github.com/MojtabaKiani/Cimon.Net/wiki

## Supported PLCs
Complete range of Cimon PLC products including `PLC-S`, `CP`, `XP` series

## Compile
You need at least Visual Studio 2019 (you can download the Community Edition for free).

## Running the tests
I used my library [Rony.Net](https://github.com/MojtabaKiani/Rony.Net) in unit tests for `EthernetConnector`. But for `SerialConnector`
I used no device or library and it only works with a fake socket.
