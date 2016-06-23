using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TXCommunication.Packets;
using TXTCommunication.Utils;

namespace TXCommunication
{
    /// <summary>
    /// The TxCommunication class is responsible for the raw communication to the ROBO TX Controller
    /// </summary>
    class TxCommunication
    {
        // We do all communication operations in one separate thread to ensure the packets are
        // sent in the correct order
        private readonly TaskQueue _communicationTaskQueue;

        // This adapter is used to sent and receive the packets
        private readonly IRfcommAdapter _adapter;

        // We cache the controller names
        private readonly IDictionary<string, string> _controllerNameCache;

        public bool Connected { get; private set; }
        
        // The sessionId is a number which is used by the TX Controller. We always have to sent back the last session id
        public short SessionId { get; private set; }

        public TxCommunication(IRfcommAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            _communicationTaskQueue = new TaskQueue("TX Communication");
            _controllerNameCache = new ConcurrentDictionary<string, string>();

            _adapter = adapter;
        }

        /// <summary>
        /// Opens the connection
        /// <exception cref="InvalidOperationException">Invalid operation is thrown when we are already connected</exception>
        /// <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException is thrown when the given mac is null or empty</exception>
        /// </summary>
        /// <param name="mac">The mac address to connect to</param>
        public void OpenConnection(string mac)
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            if (string.IsNullOrWhiteSpace(mac))
            {
                throw new ArgumentOutOfRangeException(nameof(mac));
            }

            Exception exception = null;

            // Connect in separate task
            _communicationTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    _adapter.OpenConnection(mac);
                    Connected = true;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }, true);

            // If any exception occured, we throw it
            if (exception != null)
            {
                Connected = false;
                throw exception;
            }
        }

        /// <summary>
        /// Closes the connection
        /// <exception cref="InvalidOperationException">Invalid operation is thrown when we are not connected</exception>
        /// </summary>
        public void CloseConnection()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not connected!");
            }

            Exception exception = null;

            // Do the disconnection in a separate task
            _communicationTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    _adapter.CloseConnection();
                    Connected = false;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }, true);

            // If any exception occured, we throw it
            if (exception != null)
            {
                Connected = false;
                throw exception;
            }
        }

        /// <summary>
        /// Sends a packet to the connected serial adapter
        /// <exception cref="InvalidOperationException">Invalid operation is thrown when we are not connected</exception>
        /// <exception cref="ArgumentNullException">Is thrown when an argument is null</exception>
        /// <param name="packet">The packet to send</param>
        /// <param name="responsePacket">The packet which will be received</param>
        /// </summary>
        public void SendPacket(Packet packet, Packet responsePacket)
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not Connected!");
            }

            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }
            if (responsePacket == null)
            {
                throw new ArgumentNullException(nameof(responsePacket));
            }


            Exception exception = null;

            _communicationTaskQueue.DoWorkInQueue(() =>
            {
                try
                {
                    // Set the session id. This has to be the last session id which the TX Controller has sent
                    packet.SessionId = SessionId;

                    // Write the bytes of the packet
                    _adapter.Write(packet.GetByteArray());

                    // Read the response packet
                    byte[] response = _adapter.Read(responsePacket.GetPacketLength());

                    responsePacket.FromByteArray(response);

                    // Save the last session id
                    SessionId = responsePacket.SessionId;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }, true);


            // If any exception occured, we throw it
            if (exception != null)
            {
                throw exception;
            }
        }
        
        /// <summary>
        /// Requests the controller name of the controller with the given address. It does not need an open connection
        /// <param name="address">The address which we want to connect to</param>
        /// <returns>The controller name as string. string.Empty when it failed</returns>
        /// </summary>
        public string RequestControllerName(string address)
        {
            // If we cached the address already: return the controller name
            if (_controllerNameCache.ContainsKey(address))
            {
                return _controllerNameCache[address];
            }


            try
            {
                // Open the connection
                _adapter.OpenConnection(address);

                // Write the packets bytes
                _adapter.Write(new RequestInfoPacket().GetByteArray());
                

                var responsePacket = new RequestInfoResponsePacket();
                
                // Read the response packet
                byte[] response = _adapter.Read(responsePacket.GetPacketLength());
                responsePacket.FromByteArray(response);
                
                // Close the connection and dispose the adapter
                _adapter.CloseConnection();
                _adapter.Dispose();


                _controllerNameCache.Add(address, responsePacket.ControllerName);

                return responsePacket.ControllerName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Checks if it is a valid controller
        /// <param name="address">The address which we want to connect to</param>
        /// <returns>true when valid otherwise false</returns>
        /// </summary>
        public bool IsValidInterface(string address)
        {
            // If we can request the controller name this is a valid interface
            return !string.IsNullOrEmpty(RequestControllerName(address));
        }

        public void Dispose()
        {
            ((IDisposable)_communicationTaskQueue).Dispose();
        }
    }
}
