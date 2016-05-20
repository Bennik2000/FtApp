using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using TXTCommunication.Fischertechnik.Txt.Command;
using TXTCommunication.Fischertechnik.Txt.Response;
using TXTCommunication.Utils;

namespace TXTCommunication.Fischertechnik.Txt.Camera
{
    class TxtCameraCommunication : IDisposable
    {
        private bool Connected { get; set; }
        private string IpAdress { get; set; }

        private bool RequestedStop { get; set; }

        private Socket _socket;

        private ConcurrentQueue<ResponseCameraFrame> ReceivedFrames { get; }

        // We do all the network communication in one thread with this queue
        private readonly TaskQueue _networkingTaskQueue;

        // We do the processing of the frames in one separate thread that we do not block the network thread
        private readonly TaskQueue _frameProcessingTaskQueue;
        
        private TxtCommunication TxtCommunication { get; }


        public delegate void FrameReceivedEventHandler(object sender, FrameReceivedEventArgs e);
        public event FrameReceivedEventHandler FrameReceived;

        public TxtCameraCommunication(TxtCommunication txtCommunication)
        {
            TxtCommunication = txtCommunication;
            IpAdress = TxtCommunication.IpAdress;

            ReceivedFrames = new ConcurrentQueue<ResponseCameraFrame>();
            _networkingTaskQueue = new TaskQueue("TXT Camera communication");
            _frameProcessingTaskQueue = new TaskQueue(ThreadPriority.AboveNormal, "TXT Camera frame processing", false);
        }
        
        public void StartCamera()
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            RequestedStop = false;

            TxtCommunication.SendCommand(new CommandStartCamera(), new ResponseStartCamera());

            _networkingTaskQueue.DoWorkInQueue(ConnectToCameraServerMethod, true);
            _networkingTaskQueue.DoWorkInQueue(CameraReceiverMethod, false);
        }

        public void StopCamera()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not connected!");
            }
            
            TxtCommunication.SendCommand(new CommandStopCamera(), new ResponseStopCamera());

            RequestedStop = true;

            _networkingTaskQueue.DoWorkInQueue(DisconnectFromCameraServerMethod, true);
        }
        
        

        private void ConnectToCameraServerMethod()
        {
            int tries = 0;
            while (tries < 2) // Try 2 times to connect to the camera server
            {
                try
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAdress), TxtInterface.ControllerCameraIpPort);
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(ipEndPoint);
                }
                catch (SocketException)
                {
                    Connected = false;

                    tries++;
                    Thread.Sleep(200);

                    continue;
                }
                catch (SecurityException)
                {
                    Connected = false;

                    tries++;

                    continue;
                }

                Connected = _socket.Connected;

                break;
            }
        }

        private void CameraReceiverMethod()
        {
            while (!RequestedStop)
            {
                try
                {
                    var responseCameraFrame = new ResponseCameraFrame();

                    var responseBytes = new byte[responseCameraFrame.GetResponseLength()];

                    // Receive the first part of the frame. This part contains the informations like height, width or length
                    Receive(_socket, responseBytes);

                    try
                    {
                        responseCameraFrame.FromByteArray(responseBytes);
                    }
                    catch (InvalidOperationException) // Error while receiving one frame. Close camera server
                    {
                        RequestedStop = true;
                        TxtCommunication.SendCommand(new CommandStopCamera(), new ResponseStopCamera());

                        DisconnectFromCameraServerMethod();
                        break;
                    }
                    
                    
                    var framedata = new byte[responseCameraFrame.FrameSizeCompressed];


                    // Receive the second part of the frame. This part contains the compressed JPEG data
                    Receive(_socket, framedata);

                    // Store the received frame in the responseCameraFrame object
                    responseCameraFrame.FrameData = framedata;

                    // Process the received frame in another thread queue so that we can continue receiving frames
                    ReceivedFrames.Enqueue(responseCameraFrame);
                    _frameProcessingTaskQueue.DoWorkInQueue(() =>
                    {
                        if (!ReceivedFrames.IsEmpty)
                        {
                            ResponseCameraFrame frame;
                            if (ReceivedFrames.TryDequeue(out frame))
                            {
                                FrameReceivedEventArgs eventArgs = new FrameReceivedEventArgs(frame);
                                FrameReceived?.Invoke(this, eventArgs);
                            }

                        }
                    }, false);


                    // Send an acknowledge
                    _socket.Send(BitConverter.GetBytes(TxtInterface.AcknowledgeIdCameraOnlineFrame));
                }
                catch (Exception e)
                {
                }
            }
        }

        private int Receive(Socket socket, byte[] buffer)
        {
            // Wait until enough bytes are received
            while (socket.Available < buffer.Length && !RequestedStop)
            {
                Thread.Sleep(5);
            }

            return socket.Receive(buffer);
        }

        private void DisconnectFromCameraServerMethod()
        {
            _socket.Close();
            Connected = false;
        }
        

        public void Dispose()
        {
            if (Connected)
            {
                StopCamera();
            }

            ((IDisposable)_networkingTaskQueue)?.Dispose();
            ((IDisposable)_frameProcessingTaskQueue)?.Dispose();

            ((IDisposable) _socket)?.Dispose();
        }
    }
}
