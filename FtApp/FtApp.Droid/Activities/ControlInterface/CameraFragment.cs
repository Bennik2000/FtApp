using System;
using System.ComponentModel;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControlInterface
{
    public class CameraFragment : Fragment, IFtInterfaceFragment
    {
        private ImageView _imageViewCameraStream;
        private ImageButton _imageButtonTakePicture;
        
        private bool _eventsHooked;
        private bool _firstFrame;
        

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

            _firstFrame = true;
            _imageButtonTakePicture.Click += ImageButtonTakePictureOnClick;
            
            return view;
        }
        
        public override void OnDetach()
        {
            base.OnDetach();
            UnhookEvents();
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            _firstFrame = true;

            HookEvents();
        }
        

        private void HookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null && !_eventsHooked)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded -= FtInterfaceCameraProxyOnCameraFrameDecoded;
                FtInterfaceCameraProxy.CameraFrameDecoded += FtInterfaceCameraProxyOnCameraFrameDecoded;

                FtInterfaceCameraProxy.ImageBitmapCleanup -= FtInterfaceCameraProxyOnImageBitmapCleanup;
                FtInterfaceCameraProxy.ImageBitmapCleanup += FtInterfaceCameraProxyOnImageBitmapCleanup;

                FtInterfaceCameraProxy.ImageBitmapInitialized -= FtInterfaceCameraProxyOnImageBitmapInitialized;
                FtInterfaceCameraProxy.ImageBitmapInitialized += FtInterfaceCameraProxyOnImageBitmapInitialized;

                _eventsHooked = true;
            }
        }

        private void UnhookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded -= FtInterfaceCameraProxyOnCameraFrameDecoded;
                FtInterfaceCameraProxy.ImageBitmapCleanup -= FtInterfaceCameraProxyOnImageBitmapCleanup;
                FtInterfaceCameraProxy.ImageBitmapInitialized -= FtInterfaceCameraProxyOnImageBitmapInitialized;

                _eventsHooked = false;
            }
        }

        private void InitializeCameraView()
        {
            Activity.RunOnUiThread(() =>
            {
                _imageViewCameraStream?.SetImageBitmap(FtInterfaceCameraProxy.ImageBitmap);
                _imageViewCameraStream?.Invalidate();
                _firstFrame = false;
            });
        }

        private void CleanupCameraView()
        {
            Activity.RunOnUiThread(() =>
            {
                _imageViewCameraStream?.SetImageBitmap(null);
                _imageViewCameraStream?.Invalidate();
            });
        }


        private void FtInterfaceCameraProxyOnImageBitmapCleanup(object sender, EventArgs eventArgs)
        {
            CleanupCameraView();
        }

        private void FtInterfaceCameraProxyOnImageBitmapInitialized(object sender, EventArgs eventArgs)
        {
            InitializeCameraView();
        }

        private void FtInterfaceCameraProxyOnCameraFrameDecoded(object sender, FrameDecodedEventArgs eventArgs)
        {
            Activity?.RunOnUiThread(() =>
            {
                if (_imageViewCameraStream != null && FtInterfaceCameraProxy.ImageBitmap != null)
                {
                    if (_firstFrame && !FtInterfaceCameraProxy.ImageBitmap.IsRecycled)
                    {
                        InitializeCameraView();
                    }
                    else if (!FtInterfaceCameraProxy.ImageBitmap.IsRecycled)
                    {
                        _imageViewCameraStream?.Invalidate();
                    }
                }
            });
        }


        private void ImageButtonTakePictureOnClick(object sender, EventArgs eventArgs)
        {
            var imageName = DateTime.Now.ToString("MM/dd/yyyy_HH_mm_ss") + ".jpg";

            MediaStore.Images.Media.InsertImage(Context.ContentResolver, FtInterfaceCameraProxy.ImageBitmap, imageName, imageName);

            Toast.MakeText(Activity, Resource.String.ControlInterfaceActivity_pictureTakenToast, ToastLength.Short).Show();
        }
        
        public string GetTitle(Context context)
        {
            return context.GetString(Resource.String.ControlInterfaceActivity_tabCameraTitle);
        }
    }
}