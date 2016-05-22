using Android.Animation;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using FtApp.Droid.Activities.ControlTxt;
using System.Net;
using FtApp.Utils;
using TXTCommunication.Fischertechnik.Txt;

namespace FtApp.Droid.Activities.SelectDevice
{
    public class TxtFragment : Fragment
    {
        private string _ip;
        private EditText _editTextIp;
        private RadioButton _radioButtonWifi;
        private RadioButton _radioButtonBluetooth;
        private RadioButton _radioButtonCustom;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _ip = string.Empty;
        }
        

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.SelectTxtFragmentLayout, container, false);
            
            SetupRadioButtonBehaviour(view, inflater);

            return view;
        }

        private void SetupRadioButtonBehaviour(View view, LayoutInflater inflater)
        {
            _editTextIp = view.FindViewById<EditText>(Resource.Id.connectTxtIpAdress);
            var editTextIpLayout = view.FindViewById<TextInputLayout>(Resource.Id.connectTxtIpAdressTextInputLayout);


            _radioButtonWifi = view.FindViewById<RadioButton>(Resource.Id.radioButtonWifi);
            _radioButtonBluetooth = view.FindViewById<RadioButton>(Resource.Id.radioButtonBluetooth);
            _radioButtonCustom = view.FindViewById<RadioButton>(Resource.Id.radioButton_custom);

            var buttonConnect = view.FindViewById<Button>(Resource.Id.buttonConnect);

            _radioButtonWifi.CheckedChange += delegate
            {
                if (_radioButtonWifi.Checked)
                {
                    FadeOutAnimation(editTextIpLayout);
                }
            };

            _radioButtonBluetooth.CheckedChange += delegate
            {
                if (_radioButtonBluetooth.Checked)
                {
                    FadeOutAnimation(editTextIpLayout);
                }
            };

            _radioButtonCustom.CheckedChange += delegate
            {
                if (_radioButtonCustom.Checked)
                {
                    _editTextIp.Visibility = ViewStates.Visible;
                    FadeInAnimation(editTextIpLayout);


                    // Show the soft keyboard
                    InputMethodManager inputManager = (InputMethodManager)inflater.Context.GetSystemService(Context.InputMethodService);
                    inputManager.ShowSoftInput(_editTextIp, ShowFlags.Forced);
                    inputManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);

                    _editTextIp.RequestFocus();
                }
                else
                {
                    // Hide the soft keyboard
                    InputMethodManager inputManager = (InputMethodManager)inflater.Context.GetSystemService(Context.InputMethodService);
                    inputManager.HideSoftInputFromWindow(_editTextIp.WindowToken, HideSoftInputFlags.None);
                }
            };

            buttonConnect.Click += delegate
            {
                ConnectToInterface();
            };

            _radioButtonWifi.Checked = true;
        }

        private void FadeOutAnimation(View view)
        {
            if (view.Visibility == ViewStates.Visible)
            {
                view.Animate()
                    .TranslationY(0)
                    .Alpha(0.0f)
                    .SetListener(new FadeOutAnimationListener() { AnimatedView = view });
            }
        }

        private void FadeInAnimation(View view)
        {
            if (view.Visibility != ViewStates.Visible )
            {
                view.Visibility = ViewStates.Visible;
                view.Alpha = 1;
            }
        }

        private void ConnectToInterface()
        {
            // Get the ip adress which should be used
            if (_radioButtonBluetooth.Checked)
            {
                _ip = TxtInterface.ControllerBluetoothIp;
            }
            else if (_radioButtonWifi.Checked)
            {
                _ip = TxtInterface.ControllerWifiIp;
            }
            else
            {
                if (_editTextIp == null)
                {
                    return;
                }

                _ip = _editTextIp.Text;

                if (string.IsNullOrEmpty(_ip))
                {
                    Toast.MakeText(Activity, Resource.String.SelectDeviceActivity_noIp, ToastLength.Short).Show();
                    return;
                }
            }
            

            if (NetworkUtils.IsValidIpAdress(_ip))
            {
                // Open the control activity and pass the ip adress
                Intent intent = new Intent(Activity, typeof(ControlTxtActivity));
                intent.PutExtra(ControlTxtActivity.IpAdressExtraDataId, _ip);

                StartActivity(intent);
            }
            else
            {
                string message = GetString(Resource.String.SelectDeviceActivity_notValidIp, _ip);
                Toast.MakeText(Activity, message, ToastLength.Long).Show();
            }
        }

        private class FadeOutAnimationListener : AnimatorListenerAdapter
        {
            public View AnimatedView { private get; set; }

            public override void OnAnimationEnd(Animator animation)
            {
                base.OnAnimationEnd(animation);
                AnimatedView.Visibility = ViewStates.Gone;
            }
        }
    }
}