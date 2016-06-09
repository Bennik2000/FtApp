using System;

namespace TXCommunication
{
    public interface IRfcommAdapter : IDisposable
    {
        void OpenConnection(string adress);
        void CloseConnection();
        void Write(byte[] bytes);
        bool IsAvaliable(string adress);
        byte[] Read(int count);
    }

    public delegate void SerialSearchStarted();
    public delegate void SerialSearchFound(string adress);
    public delegate void SerialSearchFinished();
}