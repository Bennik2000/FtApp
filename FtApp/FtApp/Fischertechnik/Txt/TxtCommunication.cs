using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

        private bool _requestedStop;

        private readonly IDictionary<string, string> _controllerNameCache;

        
        private Socket _socket;

        public TxtInterface TxtInterface { get; private set; }

        // We do all the network communication in one thread with this queue
        private readonly TaskQueue _networkingTaskQueue;

        public TxtCommunication(TxtInterface txtInterface)
        {
            TxtInterface = txtInterface;

            _networkingTaskQueue = new TaskQueue("TXT Communication");
            _controllerNameCache = new ConcurrentDictionary<string, string>();
        }
        
        public void OpenConnection()
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _requestedStop = false;

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
                    Connected = _socket.Connected;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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

        public void CloseConnection()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not connected!");
            }

            _requestedStop = true;

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
                    Console.WriteLine(e.Message);
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
                    Receive(_socket, responseBytes, responseBytes.Length);


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
                    Console.WriteLine(ex.Message);
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

        private int Receive(Socket socket, byte[] buffer, int count)
        {
            // Wait until enough bytes are received
            while (socket.Available < count && !_requestedStop)
            {
                Thread.Sleep(5);
            }

            return socket.Receive(buffer, 0, count, SocketFlags.None);
        }


        public string RequestControllerName(string address)
        {
            // If we cached the address already: return the controller name
            if (_controllerNameCache.ContainsKey(address))
            {
                return _controllerNameCache[address];
            }


            try
            {
                // Connect to the interface
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(address), TxtInterface.ControllerIpPort);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 1000,
                    ReceiveTimeout = 1000
                };
                socket.Connect(ipEndPoint);


                // Send the command
                socket.Send(new CommandQueryStatus().GetByteArray());

                var response = new ResponseQueryStatus();

                var responseBytes = new byte[response.GetResponseLength()];


                // Receive the response
                Receive(socket, responseBytes, responseBytes.Length);

                // Close the socket
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();

                // Process the response
                response.FromByteArray(responseBytes);
                
                _controllerNameCache.Add(address, response.GetControllerName());

                return response.GetControllerName();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public bool IsValidInterface(string address)
        {
            return !string.IsNullOrEmpty(RequestControllerName(address));
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
