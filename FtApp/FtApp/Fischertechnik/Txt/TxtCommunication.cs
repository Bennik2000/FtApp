using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using TXTCommunication.Fischertechnik.Txt.Command;
using TXTCommunication.Fischertechnik.Txt.Response;
using TXTCommunication.Utils;

namespace TXTCommunication.Fischertechnik.Txt
{
    /// <summary>
    /// This class manages the TCP/IP communication between the TXT and us.
    /// </summary>
    //TODO: Implement timeout
    class TxtCommunication : IDisposable
    {
        public bool Connected { get; private set; }
        public string IpAdress { get; private set; }


        private Socket _socket;

        // We do all the network communication in one thread with this queue
        private readonly TaskQueue _networkingTaskQueue;


        public TxtCommunication(string ipAdress)
        {
            IpAdress = ipAdress;

            _networkingTaskQueue = new TaskQueue("TXT Communication");
        }

        public void OpenConnection()
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _networkingTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAdress), TxtInterface.ControllerIpPort);
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(ipEndPoint);
                }
                catch (SocketException)
                {
                    Connected = false;
                }
                catch (SecurityException)
                {
                    Connected = false;
                }

                Connected = _socket.Connected;
            }, true);
        }

        public void CloseConnection()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not connected!");
            }

            _networkingTaskQueue.DoWorkInQueue(() =>
            {
                _socket.Close();
                Connected = false;
            }, true);
        }

        public void SendCommand(CommandBase command, ResponseBase response)
        {
            Exception exteption = null;
            
            _networkingTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    // Send the command
                    _socket.Send(command.GetByteArray());


                    var responseBytes = new byte[response.GetResponseLength()];

                    // Receive the response
                    _socket.Receive(responseBytes);


                    uint responseId = BitConverter.ToUInt32(responseBytes, 0);

                    if (responseId != response.ResponseId)
                    {
                        // Set an exception when the received response id is not the same as the expected response id
                        // The exception is thrown at the end
                        exteption =
                            new InvalidOperationException(
                                $"The response id ({responseId}) is not the same response id ({response.ResponseId}) as expected");
                    }
                    else
                    {
                        // Set the values of the given response object
                        response.FromByteArray(responseBytes);
                    }
                }
                catch (Exception ex)
                {
                    exteption = ex;
                }

            }, true);


            if (exteption != null)
            {
                throw exteption;
            }
        }
        
        public void Dispose()
        {
            // Close the connection before disposing the task queue
            if (Connected)
            {
                CloseConnection();
            }

            ((IDisposable) _networkingTaskQueue).Dispose();
            ((IDisposable) _socket).Dispose();
        }
    }
}
