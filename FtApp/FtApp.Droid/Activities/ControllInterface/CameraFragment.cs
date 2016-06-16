using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using System;
using System.ComponentModel;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Fischertechnik.Txt.Camera;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControllInterface
{
    public class CameraFragment : Fragment, IFtInterfaceFragment
    {
        private ImageView _imageViewCameraStream;
        private ImageButton _imageButtonTakePicture;

        private Bitmap _frameBitmap;

        private bool _firstFrameReceived;
        private bool _eventsHooked;
        private bool _attached;
        
        private BitmapFactory.Options _bitmapOptions;

        public bool DisplayFrames { get; } = true;

        public CameraFragment()
        {
            FtInterfaceInstanceProvider.InstanceChanged += FtInterfaceInstanceProviderOnInstanceChanged;

            RetainInstance = true;
        }

        private void FtInterfaceInstanceProviderOnInstanceChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _eventsHooked = false;
            HookEvents();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FragmentCameraLayout, container, false);

            _imageViewCameraStream = view.FindViewById<ImageView>(Resource.Id.imageViewCameraStream);
            _imageButtonTakePicture = view.FindViewById<ImageButton>(Resource.Id.imageButtonTakePicture);

            _imageButtonTakePicture.Click += ImageButtonTakePictureOnClick;

            _firstFrameReceived = false;

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

            if (txtInterface?.TxtCamera != null)
            {
                txtInterface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;
            }

        }

        public override void OnDetach()
        {
            _attached = false;

            base.OnDetach();

            UnhookEvents();

            _imageViewCameraStream.SetImageBitmap(null);
            _frameBitmap.Recycle();
            _frameBitmap.Dispose();
            _frameBitmap = null;
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            _firstFrameReceived = false;


            _bitmapOptions = new BitmapFactory.Options
            {
                InMutable = true
            };

            HookEvents();


            _attached = true;
        }
        

        private void HookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null && !_eventsHooked)
            {
                FtInterfaceInstanceProvider.Instance.Connected += FtInterfaceOnConnected;
                FtInterfaceInstanceProvider.Instance.OnlineStopped += FtInterfaceOnOnlineStopped;


                TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

                if (txtInterface?.TxtCamera != null)
                {
                    txtInterface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;
                    txtInterface.TxtCamera.FrameReceived += TxtCameraOnFrameReceived;
                }

                _eventsHooked = true;
            }
        }

        private void UnhookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceInstanceProvider.Instance.Connected -= FtInterfaceOnConnected;
                FtInterfaceInstanceProvider.Instance.OnlineStopped -= FtInterfaceOnOnlineStopped;


                TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

                if (txtInterface?.TxtCamera != null)
                {
                    txtInterface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;
                }

                _eventsHooked = false;
            }
        }

        private void ImageButtonTakePictureOnClick(object sender, EventArgs eventArgs)
        {
            var imageName = DateTime.Now.ToString("MM/dd/yyyy_HH_mm_ss") + ".jpg";

            MediaStore.Images.Media.InsertImage(Context.ContentResolver, _frameBitmap, imageName, imageName);

            Toast.MakeText(Activity, Resource.String.ControlTxtActivity_pictureTakenToast, ToastLength.Short).Show();
        }
        
        private void FtInterfaceOnOnlineStopped(object sender, EventArgs eventArgs)
        {
            StopCameraStream();
        }

        private void FtInterfaceOnConnected(object sender, EventArgs eventArgs)
        {
            TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

            if (txtInterface != null)
            {
                StartCameraStream();
                txtInterface.TxtCamera.FrameReceived -= TxtCameraOnFrameReceived;
                txtInterface.TxtCamera.FrameReceived += TxtCameraOnFrameReceived;
            }
        }

        public void StartCameraStream()
        {
            TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

            if (txtInterface != null)
            {
                txtInterface.TxtCamera.StartCamera();
            }
        }

        public void StopCameraStream()
        {
            TxtInterface txtInterface = FtInterfaceInstanceProvider.Instance as TxtInterface;

            if (txtInterface != null)
            {
                txtInterface.TxtCamera.StopCamera();
                _firstFrameReceived = false;
            }

        }

        private void TxtCameraOnFrameReceived(object sender, FrameReceivedEventArgs frameReceivedEventArgs)
        {
            if (_imageViewCameraStream != null && _attached)
            {
                if (DisplayFrames)
                {
                    if (_frameBitmap != null && !_frameBitmap.IsRecycled && _firstFrameReceived)
                    {
                        _bitmapOptions.InBitmap = _frameBitmap;
                    }

                    _frameBitmap = BitmapFactory.DecodeByteArray(frameReceivedEventArgs.FrameData, 0,
                        frameReceivedEventArgs.DataLength, _bitmapOptions);

                    Activity.RunOnUiThread(() =>
                    {
                        if (!_firstFrameReceived)
                        {
                            _imageViewCameraStream.SetImageBitmap(_frameBitmap);
                        }
                        _firstFrameReceived = true;

                        _imageViewCameraStream.Invalidate();
                    });

                }
            }
        }

        public string GetTitle(Context context)
        {
            return context.GetString(Resource.String.ControlTxtActivity_tabCameraTitle);
        }
    }
}