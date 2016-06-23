using System;
using System.ComponentModel;
using Android.Graphics;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Fischertechnik.Txt.Camera;

namespace FtApp.Droid.Activities.ControlInterface
{
    /// <summary>
    /// This static class handles the communication with the camera of the TXT Controller
    /// </summary>
    internal static class FtInterfaceCameraProxy
    {
        /// <summary>
        /// This event is fired when a new frame arrived and is stored in the ImageBitmap object
        /// </summary>
        public static event CameraFrameDecodedEventHandler CameraFrameDecoded;
        public delegate void CameraFrameDecodedEventHandler(object sender, FrameDecodedEventArgs eventArgs);

        /// <summary>
        /// This event is fired when the bitmap was cleaned up and is now recycled
        /// </summary>
        public static event ImageBitmapCleanupEventHandler ImageBitmapCleanup;
        public delegate void ImageBitmapCleanupEventHandler(object sender, EventArgs eventArgs);
        
        /// <summary>
        /// This event is fired when the bitmap was initialized and contains data
        /// </summary>
        public static event ImageBitmapInitializedEventHandler ImageBitmapInitialized;
        public delegate void ImageBitmapInitializedEventHandler(object sender, EventArgs eventArgs);


        private static TxtInterface Interface { get; set; }

        /// <summary>
        /// This bitmap object contains the actual frame
        /// </summary>
        public static Bitmap ImageBitmap { get; private set; }

        private static BitmapFactory.Options ImageOptions { get; set; }

        /// <summary>
        /// true when the first frame arrived otherwise false
        /// </summary>
        public static bool FirstFrame { get; private set; }

        /// <summary>
        /// true when a camera is available otherwise false
        /// </summary>
        public static bool CameraAvailable { get; private set; }

        /// <summary>
        /// true when the camera stream is running otherwise false
        /// </summary>
        public static bool CameraStreaming { get; private set; }

        static FtInterfaceCameraProxy()
        {
            FtInterfaceInstanceProvider.InstanceChanged += FtInterfaceInstanceProviderOnInstanceChanged;
        }


        private static void FtInterfaceInstanceProviderOnInstanceChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (FtInterfaceInstanceProvider.Instance == null)
            {
                // Clean the bitmap memory when the instance is null
                CleanupBitmapMemory();
                return;
            }
            
            Interface = FtInterfaceInstanceProvider.Instance as TxtInterface;
            CameraAvailable = Interface != null;
        }

        private static void TxtCameraOnFrameReceived(object sender, FrameReceivedEventArgs frameReceivedEventArgs)
        {
            // When a new frame arrived we decode it and fire an event
            DecodeBitmap(frameReceivedEventArgs.FrameData, frameReceivedEventArgs.DataLength);

            CameraFrameDecoded?.Invoke(null, new FrameDecodedEventArgs(FirstFrame));

            FirstFrame = false;
        }

        /// <summary>
        /// Starts the camera stream
        /// </summary>
        public static void StartCameraStream()
        {
            // ReSharper disable once UseNullPropagation
            if (CameraAvailable && Interface != null && !CameraStreaming)
            {
                if (Interface.TxtCamera != null)
                {
                    CameraStreaming = true;

                    SetupBitmap();

                    FirstFrame = true;

                    Interface.TxtCamera.FrameReceived += TxtCameraOnFrameReceived;
                    Interface.TxtCamera.StartCamera();
                }
            }
        }

        /// <summary>
        /// Stops the camera stream
        /// </summary>
        public static void StopCameraStream()
        {
            if (CameraAvailable && Interface != null && CameraStreaming)
            {
                if (Interface.TxtCamera != null)
                {
                    try
                    {
                        CameraStreaming = false;
                        Interface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;

                        // Stop the stream and cleanup the frame bitmap
                        Interface.TxtCamera.StopCamera();

                        CleanupBitmapMemory();
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch (Exception) { }
                }
            }
        }


        private static void DecodeBitmap(byte[] bytes, int length)
        {
            if (ImageBitmap != null && !ImageBitmap.IsRecycled && !FirstFrame)
            {
                // When the bitmap is initialized and not recycled we set it as InBitmap in the ImageOptions to reuse the frame memory of the frame before
                ImageOptions.InBitmap = ImageBitmap;
            }

            // We decode the frame using ImageOptions
            ImageBitmap = BitmapFactory.DecodeByteArray(bytes, 0, length, ImageOptions);

            if (FirstFrame)
            {
                ImageBitmapInitialized?.Invoke(null, EventArgs.Empty);
            }
        }

        private static void SetupBitmap()
        {
            // First clean the bitmap memory to prevent memory leaks
            CleanupBitmapMemory();
            ImageOptions = new BitmapFactory.Options
            {
                InMutable = true
            };
        }

        private static void CleanupBitmapMemory()
        {
            if (ImageBitmap != null)
            {
                ImageBitmapCleanup?.Invoke(null, EventArgs.Empty);

                // Recycle and Dispose the bitmap
                if (!ImageBitmap.IsRecycled)
                {
                    ImageBitmap.Recycle();
                }
                ImageBitmap.Dispose();
                ImageBitmap = null;
            }
            if (ImageOptions != null)
            {
                ImageOptions.Dispose();
                ImageBitmap = null;
            }
        }
    }

    internal class FrameDecodedEventArgs : EventArgs
    {
        public readonly bool FirstFrame;

        internal FrameDecodedEventArgs(bool firstFrame)
        {
            FirstFrame = firstFrame;
        }
    }
}