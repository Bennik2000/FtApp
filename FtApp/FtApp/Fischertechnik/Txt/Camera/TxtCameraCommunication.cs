using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        private string Ipaddress { get; set; }

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
            Ipaddress = TxtCommunication.TxtInterface.Ip;

            ReceivedFrames = new ConcurrentQueue<ResponseCameraFrame>();
            _networkingTaskQueue = new TaskQueue("TXT Camera communication");
            _frameProcessingTaskQueue = new TaskQueue(ThreadPriority.AboveNormal, "TXT Camera frame processing", false);
        }
        
        public void StartCamera([CallerMemberName]string memberName = "")
        {
            TxtCommunication.TxtInterface.LogMessage("Starting camera" + memberName);
            if (Connected)
            {
                //throw new InvalidOperationException("Already connected!");
                return;
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
                //throw new InvalidOperationException("Not connected!");
                return;
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
                TxtCommunication.TxtInterface.LogMessage("Trying to connect to camera server");
                try
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(Ipaddress), TxtInterface.ControllerCameraIpPort);
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(ipEndPoint);
                    
                    TxtCommunication.TxtInterface.LogMessage("Connected to camera server");
                }
                catch (SocketException e)
                {
                    TxtCommunication.TxtInterface.LogMessage($"Exception while connecting to camera server: {e.Message}");
                    Connected = false;

                    tries++;
                    Thread.Sleep(200);

                    continue;
                }
                catch (SecurityException e)
                {
                    TxtCommunication.TxtInterface.LogMessage($"Exception while connecting to camera server: {e.Message}");

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
            byte[] framedata = new byte[1];

            var responseCameraFrame = new ResponseCameraFrame();


            while (!RequestedStop)
            {
                try
                {
                    var responseBytes = new byte[responseCameraFrame.GetResponseLength()];

                    // Receive the first part of the frame. This part contains the informations like height, width or length
                    Receive(_socket, responseBytes, responseBytes.Length);

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


                    // Use the existing framedata object and resize if needed
                    if (framedata.Length < responseCameraFrame.FrameSizeCompressed + 2)
                    {
                        Array.Resize(ref framedata, responseCameraFrame.FrameSizeCompressed + 2);
                    }


                    // Receive the second part of the frame. This part contains the compressed JPEG data
                    Receive(_socket, framedata, responseCameraFrame.FrameSizeCompressed);

                    // Add the missing EOI (End of image) tag
                    framedata[framedata.Length - 2] = 0xFF;
                    framedata[framedata.Length - 1] = 0xD9;

                    // Store the received frame in the responseCameraFrame object
                    responseCameraFrame.FrameData = framedata;

                    // Process the received frame in another thread queue so that we can continue receiving frames
                    ReceivedFrames.Enqueue(responseCameraFrame);
                    _frameProcessingTaskQueue.DoWorkInQueue(() =>
                    {
                        if (!ReceivedFrames.IsEmpty)
                        {
                            ResponseCameraFrame frame;
                            if (ReceivedFrames.TryDequeue(out frame) && !RequestedStop)
                            {
                                FrameReceivedEventArgs eventArgs = new FrameReceivedEventArgs(framedata, responseCameraFrame.FrameSizeCompressed + 2);
                                FrameReceived?.Invoke(this, eventArgs);
                            }

                        }
                    }, false);


                    // Send an acknowledge
                    _socket.Send(BitConverter.GetBytes(TxtInterface.AcknowledgeIdCameraOnlineFrame));
                }
                catch (Exception)
                {
                }
            }
        }

        private int Receive(Socket socket, byte[] buffer, int count)
        {
            // Wait until enough bytes are received
            while (socket.Available < count && !RequestedStop)
            {
                Thread.Sleep(5);
            }

            return socket.Receive(buffer, 0, count, SocketFlags.None);
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
