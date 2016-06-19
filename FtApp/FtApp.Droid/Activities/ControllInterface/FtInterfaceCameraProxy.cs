using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Fischertechnik.Txt.Camera;

namespace FtApp.Droid.Activities.ControllInterface
{
    internal static class FtInterfaceCameraProxy
    {
        public delegate void CameraFrameDecodedEventHandler(object sender, FrameDecodedEventArgs eventArgs);
        public static event CameraFrameDecodedEventHandler CameraFrameDecoded;

        private static TxtInterface Interface { get; set; }


        public static Bitmap ImageBitmap { get; private set; }
        private static BitmapFactory.Options ImageOptions { get; set; }

        public static bool FirstFrame { get; private set; }

        public static bool CameraPossible { get; private set; }

        public static bool CameraStreaming { get; private set; }

        static FtInterfaceCameraProxy()
        {
            FtInterfaceInstanceProvider.InstanceChanged += FtInterfaceInstanceProviderOnInstanceChanged;
        }


        private static void FtInterfaceInstanceProviderOnInstanceChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (FtInterfaceInstanceProvider.Instance == null)
            {
                CleanupBitmapMemory();
                return;
            }

            Interface = FtInterfaceInstanceProvider.Instance as TxtInterface;
            CameraPossible = Interface != null;
        }

        private static void TxtCameraOnFrameReceived(object sender, FrameReceivedEventArgs frameReceivedEventArgs)
        {
            DecodeBitmap(frameReceivedEventArgs.FrameData, frameReceivedEventArgs.DataLength);

            CameraFrameDecoded?.Invoke(null, new FrameDecodedEventArgs(FirstFrame));

            FirstFrame = false;
        }

        
        public static void StartCameraStream()
        {
            // ReSharper disable once UseNullPropagation
            if (CameraPossible && Interface != null && !CameraStreaming)
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

        public static void StopCameraStream()
        {
            if (CameraPossible && Interface != null && CameraStreaming)
            {
                if (Interface.TxtCamera != null)
                {
                    try
                    {
                        CameraStreaming = false;
                        Interface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;
                        Interface.TxtCamera.StopCamera();

                        CleanupBitmapMemory();
                    }
                    catch (Exception e)
                    {
                        
                    }
                }
            }
        }


        private static void DecodeBitmap(byte[] bytes, int length)
        {
            if (ImageBitmap != null && !ImageBitmap.IsRecycled && !FirstFrame)
            {
                ImageOptions.InBitmap = ImageBitmap;
            }
            
            ImageBitmap = BitmapFactory.DecodeByteArray(bytes, 0, length, ImageOptions);
        }

        private static void SetupBitmap()
        {
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