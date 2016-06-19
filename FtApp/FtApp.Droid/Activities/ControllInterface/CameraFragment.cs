using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using System;
using System.ComponentModel;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControllInterface
{
    public class CameraFragment : Fragment, IFtInterfaceFragment
    {
        private ImageView _imageViewCameraStream;
        private ImageButton _imageButtonTakePicture;
        
        private bool _eventsHooked;
        private bool _attached;
        

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
            
            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
        }

        public override void OnDetach()
        {
            _attached = false;

            base.OnDetach();

            UnhookEvents();

            _imageViewCameraStream.SetImageBitmap(null);
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);
            

            HookEvents();


            _attached = true;
        }
        

        private void HookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null && !_eventsHooked)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded += FtInterfaceCameraProxyOnCameraFrameDecoded;

                _eventsHooked = true;
            }
        }

        private void FtInterfaceCameraProxyOnCameraFrameDecoded(object sender, FrameDecodedEventArgs eventArgs)
        {
            if (_imageViewCameraStream != null && FtInterfaceCameraProxy.ImageBitmap != null)
            {
                Activity?.RunOnUiThread(() =>
                {
                    if (eventArgs.FirstFrame)
                    {
                        _imageViewCameraStream.SetImageBitmap(FtInterfaceCameraProxy.ImageBitmap);
                    }

                    _imageViewCameraStream.Invalidate();
                });
            }
        }

        private void UnhookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded -= FtInterfaceCameraProxyOnCameraFrameDecoded;

                _eventsHooked = false;
            }
        }

        private void ImageButtonTakePictureOnClick(object sender, EventArgs eventArgs)
        {
            var imageName = DateTime.Now.ToString("MM/dd/yyyy_HH_mm_ss") + ".jpg";

            MediaStore.Images.Media.InsertImage(Context.ContentResolver, FtInterfaceCameraProxy.ImageBitmap, imageName, imageName);

            Toast.MakeText(Activity, Resource.String.ControlTxtActivity_pictureTakenToast, ToastLength.Short).Show();
        }
        
        public string GetTitle(Context context)
        {
            return context.GetString(Resource.String.ControlTxtActivity_tabCameraTitle);
        }
    }
}