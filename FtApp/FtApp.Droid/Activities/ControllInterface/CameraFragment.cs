using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Fischertechnik.Txt.Camera;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControllInterface
{
    public class CameraFragment : Fragment, IFtInterfaceFragment
    {
        private TxtInterface _ftInterface;

        private ImageView _imageViewCameraStream;
        //private ProgressBar _progressBarRamUsage;
        private Bitmap _frameBitmap;

        private bool _firstFrameReceived;
        
        private readonly BitmapFactory.Options _bitmapOptions;

        public bool DisplayFrames { get; set; }

        public CameraFragment()
        {
            _bitmapOptions = new BitmapFactory.Options
            {
                InMutable = true
            };
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CameraFragmentLayout, container, false);

            _imageViewCameraStream = view.FindViewById<ImageView>(Resource.Id.imageViewCameraStream);
            
            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            // Cleanup the frame memory
            _frameBitmap?.Recycle();

            _firstFrameReceived = false;
        }

        public void SetFtInterface(IFtInterface ftInterface)
        {
            TxtInterface txtInterface = ftInterface as TxtInterface;
            if (txtInterface != null)
            {
                _ftInterface = txtInterface;
                _ftInterface.Connected += FtInterfaceOnConnected;
                _ftInterface.OnlineStopped += FtInterfaceOnOnlineStopped;
            }
        }

        private void FtInterfaceOnOnlineStopped(object sender, EventArgs eventArgs)
        {
            StopCameraStream();
        }

        private void FtInterfaceOnConnected(object sender, EventArgs eventArgs)
        {
            _ftInterface.TxtCamera.FrameReceived += TxtCameraOnFrameReceived;
            StartCameraStream();
        }

        public void StartCameraStream()
        {
            _ftInterface.TxtCamera.StartCamera();
        }

        public void StopCameraStream()
        {
            _ftInterface.TxtCamera.StopCamera();
            
            _firstFrameReceived = false;
        }

        private void TxtCameraOnFrameReceived(object sender, FrameReceivedEventArgs frameReceivedEventArgs)
        {
            if (DisplayFrames)
            {
                if (_frameBitmap != null && !_frameBitmap.IsRecycled)
                {
                    _bitmapOptions.InBitmap = _frameBitmap;
                }

                _frameBitmap = BitmapFactory.DecodeByteArray(frameReceivedEventArgs.FrameData, 0,
                    frameReceivedEventArgs.DataLength, _bitmapOptions);

                if (!_firstFrameReceived)
                {
                    _imageViewCameraStream.SetImageBitmap(_frameBitmap);
                }
                Activity.RunOnUiThread(_imageViewCameraStream.Invalidate);

                _firstFrameReceived = true;

            }
        }

        public string GetTitle(Context context)
        {
            return context.GetString(Resource.String.ControlTxtActivity_tabCameraTitle);
        }
    }
}