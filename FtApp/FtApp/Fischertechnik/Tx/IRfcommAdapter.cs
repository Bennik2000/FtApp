using System;

namespace TXCommunication
{
    /// <summary>
    /// This interface implements an adapter to an serial port. This can be a COM port, a bluetooth socket, tcp/ip socket, ...
    /// </summary>
    public interface IRfcommAdapter : IDisposable
    {
        void OpenConnection(string address);
        void CloseConnection();
        void Write(byte[] bytes);
        bool IsAvaliable(string address);
        byte[] Read(int count);
    }
    
    public delegate void SerialSearchStarted();
    public delegate void SerialSearchFound(string address);
    public delegate void SerialSearchFinished();
}