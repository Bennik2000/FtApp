using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TXCommunication.Packets;
using TXTCommunication.Utils;

namespace TXCommunication
{
    class TxCommunication
    {
        // We do all communication operations in one separate thread to ensure the packets are
        // sent in the correct order
        private readonly TaskQueue _communicationTaskQueue;

        private readonly IRfcommAdapter _adapter;

        private readonly IDictionary<string, string> _controllerNameCache;

        public bool Connected { get; private set; }
        
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

            if (exception != null)
            {
                Connected = false;
                throw exception;
            }
        }

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


            if (exception != null)
            {
                throw exception;
            }
        }

        public string RequestControllerName(string adress)
        {
            // If we cached the adress already: return the controller name
            if (_controllerNameCache.ContainsKey(adress))
            {
                return _controllerNameCache[adress];
            }


            try
            {
                // Open the connection
                _adapter.OpenConnection(adress);

                // Write the packets bytes
                _adapter.Write(new RequestInfoPacket().GetByteArray());
                

                var responsePacket = new RequestInfoResponsePacket();
                
                // Read the response packet
                byte[] response = _adapter.Read(responsePacket.GetPacketLength());
                responsePacket.FromByteArray(response);
                
                // Close the connection and dispose the adapter
                _adapter.CloseConnection();
                _adapter.Dispose();


                _controllerNameCache.Add(adress, responsePacket.ControllerName);

                return responsePacket.ControllerName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public bool IsValidInterface(string adress)
        {
            // If we can request the controller name this is a valid interface
            return !string.IsNullOrEmpty(RequestControllerName(adress));
        }

        public void Dispose()
        {
            ((IDisposable)_communicationTaskQueue).Dispose();
        }
    }
}
