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
    /// <summary>
    /// The camera fragment displays the camera stream of the TXT Controller in an image view
    /// </summary>
    public class CameraFragment : Fragment, ITitledFragment
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

            // Hook the events when the interface has been changed
            HookEvents();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FragmentCameraLayout, container, false);

            _imageViewCameraStream = view.FindViewById<ImageView>(Resource.Id.imageViewCameraStream);
            _imageButtonTakePicture = view.FindViewById<ImageButton>(Resource.Id.imageButtonTakePicture);

            _imageButtonTakePicture.Click += ImageButtonTakePictureOnClick;


            // Set _firstFrame to true that the image view is initialized propertly
            _firstFrame = true;
            
            return view;
        }
        
        public override void OnDetach()
        {
            base.OnDetach();

            // Unhook the events when detached
            UnhookEvents();
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            // Set _firstFrame to true that the image view is initialized propertly
            _firstFrame = true;

            // Hook the events when attached
            HookEvents();
        }
        

        private void HookEvents()
        {
            // Hooks the camera events
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
            // Unhooks the camera events
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
                // Set the bitmap which should be used
                _imageViewCameraStream?.SetImageBitmap(FtInterfaceCameraProxy.ImageBitmap);

                // Invalidate the view to display the actual stored bitmap
                _imageViewCameraStream?.Invalidate();
                _firstFrame = false;
            });
        }

        private void CleanupCameraView()
        {
            Activity.RunOnUiThread(() =>
            {
                // Clears the bitmap
                _imageViewCameraStream?.SetImageBitmap(null);

                // Invalidate the view to display the actual stored bitmap
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
                    // When this is the first frame: initialize the image view
                    if (_firstFrame && !FtInterfaceCameraProxy.ImageBitmap.IsRecycled)
                    {
                        InitializeCameraView();
                    }
                    else if (!FtInterfaceCameraProxy.ImageBitmap.IsRecycled)
                    {
                        // Invalidate the image view to display the actual stored bitmap
                        _imageViewCameraStream?.Invalidate();
                    }
                }
            });
        }


        private void ImageButtonTakePictureOnClick(object sender, EventArgs eventArgs)
        {
            var imageName = DateTime.Now.ToString("MM/dd/yyyy_HH_mm_ss") + ".jpg";

            // Store the bitmap in the gallery. This should be done in another thread
            MediaStore.Images.Media.InsertImage(Context.ContentResolver, FtInterfaceCameraProxy.ImageBitmap, imageName, imageName);

            Toast.MakeText(Activity, Resource.String.ControlInterfaceActivity_pictureTakenToast, ToastLength.Short).Show();
        }
        
        public string GetTitle(Context context)
        {
            return context.GetString(Resource.String.ControlInterfaceActivity_tabCameraTitle);
        }
    }
}