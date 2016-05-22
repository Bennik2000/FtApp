using System;
using System.Net;
using System.Net.Sockets;
using TXTCommunication.Fischertechnik.Txt.Command;
using TXTCommunication.Fischertechnik.Txt.Response;
using TXTCommunication.Utils;

namespace TXTCommunication.Fischertechnik.Txt
{
    /// <summary>
    /// This class manages the TCP/IP communication between the TXT and us.
    /// </summary>
    class TxtCommunication : IDisposable
    {
        public bool Connected { get; private set; }


        private Socket _socket;

        public TxtInterface TxtInterface { get; private set; }

        // We do all the network communication in one thread with this queue
        private readonly TaskQueue _networkingTaskQueue;


        public TxtCommunication(TxtInterface txtInterface)
        {
            TxtInterface = txtInterface;

            _networkingTaskQueue = new TaskQueue("TXT Communication");
        }

        public void OpenConnection()
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            Exception exception = null;

            _networkingTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(TxtInterface.Ip), TxtInterface.ControllerIpPort);
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        SendTimeout = TxtInterface.TcpTimeout,
                        ReceiveTimeout = TxtInterface.TcpTimeout
                    };
                    _socket.Connect(ipEndPoint);
                }
                catch (Exception e)
                {
                    exception = e;
                    Connected = false;
                }

                Connected = _socket.Connected;
            }, true);

            if (exception != null)
            {
                Connected = false;
                throw exception;
            }
        }

        public void CloseConnection()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not connected!");
            }

            Exception exception = null;

            _networkingTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                    Connected = false;
                }
                catch (SocketException e)
                {
                    exception = e;
                    Connected = false;
                }
            }, true);
            
            if (exception != null)
            {
                Connected = false;
                throw exception;
            }
        }

        public void SendCommand(CommandBase command, ResponseBase response)
        {
            Exception exception = null;
            
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
                        exception =
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
                    exception = ex;
                }

            }, true);


            if (exception != null)
            {
                if (!(exception is InvalidOperationException))
                {
                    Connected = false;
                }

                throw exception;
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
