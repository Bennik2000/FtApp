using System;
using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using FtApp.Droid.Views;
using FtApp.Utils;
using Newtonsoft.Json;
using TXTCommunication.Fischertechnik;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace FtApp.Droid.Activities.ControlInterface
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class JoystickFragment : Fragment, IFtInterfaceFragment
    {
        private JoystickView _joystickViewLeft;
        private JoystickView _joystickViewRight;

        private ImageView _imageViewCameraStream;

        private ImageView _imageViewContextualMenuLeft;
        private ImageView _imageViewContextualMenuRight;

        private JoystickConfiguration _leftJoystickConfiguration;
        private JoystickConfiguration _rightJoystickConfiguration;
        
        private const int LeftJoystickIndex = 0;
        private const int RightJoystickIndex = 1;

        private const string LeftJoystickPreferenceKey = "LeftJoystick";
        private const string RightJoystickPreferenceKey = "RightJoystick";

        bool _firstFrame = true;

        public JoystickFragment()
        {
            FtInterfaceInstanceProvider.InstanceChanged += FtInterfaceInstanceProviderOnInstanceChanged;

            _leftJoystickConfiguration = new JoystickConfiguration
            {
                JoystickMode = JoystickConfiguration.JoystickModes.Syncron,
                MotorIndexes = new[] {0, 1}
            };
            _rightJoystickConfiguration = new JoystickConfiguration
            {
                JoystickMode = JoystickConfiguration.JoystickModes.Single,
                MotorIndexes = new[] {2, 3}
            };
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FragmentJoystick, container, false);

            view.Touch += (sender, args) => { args.Handled = true; };

            _imageViewCameraStream = view.FindViewById<ImageView>(Resource.Id.joystickCameraView);

            _joystickViewLeft = view.FindViewById<JoystickView>(Resource.Id.joystickLeft);
            _joystickViewRight = view.FindViewById<JoystickView>(Resource.Id.joystickRight);
            
            _joystickViewLeft.ValuesChanged += delegate { JoystickViewOnValuesChanged(LeftJoystickIndex); };
            _joystickViewRight.ValuesChanged += delegate { JoystickViewOnValuesChanged(RightJoystickIndex); };

            _imageViewContextualMenuLeft =
                view.FindViewById<ImageView>(Resource.Id.imageViewContextualMenuJoystickLeft);

            _imageViewContextualMenuRight =
                view.FindViewById<ImageView>(Resource.Id.imageViewContextualMenuJoystickRight);
            

            _imageViewContextualMenuLeft.Click += delegate
            {
                ShowJoystickConfigurationOptionsMenu(_imageViewContextualMenuLeft, LeftJoystickIndex);
            };
            _imageViewContextualMenuRight.Click += delegate
            {
                ShowJoystickConfigurationOptionsMenu(_imageViewContextualMenuRight, RightJoystickIndex);
            };
            


            return view;
        }


        public override void OnAttach(Activity activity)
        {
            base.OnAttach(activity);

            LoadJoystickConfiguration();
            HookEvents();

            _firstFrame = true;
        }

        public override void OnDetach()
        {
            base.OnDetach();
            SaveJoystickConfiguration();
            UnhookEvents();
        }


        public void ShowJoystickConfigurationOptionsMenu(ImageView view, int joystickIndex)
        {
            var popup = new PopupMenu(Activity, view);
            popup.MenuInflater.Inflate(Resource.Menu.ConfigureJoystickPopupMenu, popup.Menu);
            popup.Show();

            popup.MenuItemClick += delegate(object sender, PopupMenu.MenuItemClickEventArgs args)
            {
                ConfigureJoystickPopupOnMenuItemClick(args.Item.ItemId, joystickIndex);
            };
        }

        private void ConfigureJoystickPopupOnMenuItemClick(int menuItemId, int joystickId)
        {
            switch (menuItemId)
            {
                case Resource.Id.menuJoystickMode:
                    ShowModeConfigurationDialog(joystickId);
                    break;
                case Resource.Id.menuJoystickMotors:
                    ShowMotorConfigurationDialog(joystickId, 0);
                    break;
            }
        }

        private void ShowMotorConfigurationDialog(int joystickId, int joystickAxis)
        {
            AlertDialog motorConfigurationDialog = null;

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AlertDialogStyle);

            builder.SetSingleChoiceItems(GetMotorList(),
                GetRelatedJoystickConfiguration(joystickId).MotorIndexes[joystickAxis],
                delegate (object sender, DialogClickEventArgs args)
                {
                    SetMotorIndex(joystickId, args.Which, joystickAxis);

                    // ReSharper disable once AccessToModifiedClosure
                    motorConfigurationDialog?.Dismiss();

                    if (joystickAxis == 0)
                    {
                        ShowMotorConfigurationDialog(joystickId, ++joystickAxis);
                    }
                });

            builder.SetPositiveButton(Resource.String.ControlInterfaceActivity_configureJoystickMotor1DialogPositive,
                delegate
                {
                    // ReSharper disable once AccessToModifiedClosure
                    motorConfigurationDialog?.Dismiss();

                    if (joystickAxis == 0)
                    {
                        ShowMotorConfigurationDialog(joystickId, ++joystickAxis);
                    }
                });

            if (joystickAxis == 0)
            {
                builder.SetTitle(Resource.String.ControlInterfaceActivity_configureJoystickMotor1DialogTitle);
            }
            else if(joystickAxis == 1)
            {
                builder.SetTitle(Resource.String.ControlInterfaceActivity_configureJoystickMotor2DialogTitle);
            }

            builder.SetCancelable(false);

            motorConfigurationDialog = builder.Create();

            motorConfigurationDialog.Show();
        }

        private void SetMotorIndex(int joystickId, int motorId, int joystickAxis)
        {
            var configuration = GetRelatedJoystickConfiguration(joystickId);

            if (configuration != null)
            {
                configuration.MotorIndexes[joystickAxis] = motorId;

                ApplyJoystickConfiguration(configuration, joystickId);
            }
        }
        
        private void ShowModeConfigurationDialog(int joystickId)
        {
            AlertDialog modeConfigurationDialog = null;

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AlertDialogStyle);

            builder.SetSingleChoiceItems(Resource.Array.JoystickModes,
                (int)GetRelatedJoystickConfiguration(joystickId).JoystickMode,
                delegate(object sender, DialogClickEventArgs args)
                {
                    JoystickModeConfigurationItemClick(args, joystickId);

                    // ReSharper disable once AccessToModifiedClosure
                    modeConfigurationDialog?.Dismiss();
                });

            builder.SetPositiveButton(Resource.String.ControlInterfaceActivity_configureJoystickModeDialogPositive,
                delegate
                {
                    // ReSharper disable once AccessToModifiedClosure
                    modeConfigurationDialog?.Dismiss();
                });


            builder.SetTitle(Resource.String.ControlInterfaceActivity_configureJoystickModeDialogTitle);
            builder.SetCancelable(false);

            modeConfigurationDialog = builder.Create();

            modeConfigurationDialog.Show();
        }

        private void JoystickModeConfigurationItemClick(DialogClickEventArgs dialogClickEventArgs, int joystickId)
        {
            var configuration = GetRelatedJoystickConfiguration(joystickId);

            if (configuration != null)
            {
                configuration.JoystickMode = (JoystickConfiguration.JoystickModes) dialogClickEventArgs.Which;

                ApplyJoystickConfiguration(configuration, joystickId);
            }
        }


        private void LoadJoystickConfiguration()
        {
            ISharedPreferences sharedPreferences = Activity.GetSharedPreferences(typeof(JoystickConfiguration).FullName, 0);

            string leftJoystickJson = sharedPreferences.GetString(LeftJoystickPreferenceKey, string.Empty);
            string rightJoystickJson = sharedPreferences.GetString(RightJoystickPreferenceKey, string.Empty);


            if (!string.IsNullOrEmpty(leftJoystickJson))
            {
                try
                {
                    _leftJoystickConfiguration = JsonConvert.DeserializeObject<JoystickConfiguration>(leftJoystickJson);
                }
                catch (JsonException) { }
            }
            if (!string.IsNullOrEmpty(rightJoystickJson))
            {
                try
                {
                    _rightJoystickConfiguration = JsonConvert.DeserializeObject<JoystickConfiguration>(rightJoystickJson);
                }
                catch (JsonException) { }
            }
        }

        private void SaveJoystickConfiguration()
        {
            // Serialite both joysticks to json strings
            string leftJoystickJson = JsonConvert.SerializeObject(_leftJoystickConfiguration);
            string rightJoystickJson = JsonConvert.SerializeObject(_rightJoystickConfiguration);

            
            // Store the strings in shared preferences
            ISharedPreferences sharedPreferences = Activity.GetSharedPreferences(typeof(JoystickConfiguration).FullName, 0);
            
            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            if (editor != null)
            {
                editor.PutString(LeftJoystickPreferenceKey, leftJoystickJson);
                editor.PutString(RightJoystickPreferenceKey, rightJoystickJson);

                editor.Commit();
            }
        }


        private JoystickConfiguration GetRelatedJoystickConfiguration(int joystickId)
        {
            switch (joystickId)
            {
                case LeftJoystickIndex:
                    return _leftJoystickConfiguration;
                case RightJoystickIndex:
                    return _rightJoystickConfiguration;
            }

            return null;
        }

        private JoystickView GetRelatedJoystickView(int joystickId)
        {
            switch (joystickId)
            {
                case LeftJoystickIndex:
                    return _joystickViewLeft;
                case RightJoystickIndex:
                    return _joystickViewRight;
            }

            return null;
        }

        private void ApplyJoystickConfiguration(JoystickConfiguration configuration, int joystickId)
        {
            switch (joystickId)
            {
                case LeftJoystickIndex:
                    _leftJoystickConfiguration = configuration;
                    break;
                case RightJoystickIndex:
                    _rightJoystickConfiguration = configuration;
                    break;
            }
        }


        private void HookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded -= FtInterfaceCameraProxyOnCameraFrameDecoded;
                FtInterfaceCameraProxy.CameraFrameDecoded += FtInterfaceCameraProxyOnCameraFrameDecoded;

                FtInterfaceCameraProxy.ImageBitmapCleanup -= FtInterfaceCameraProxyOnImageBitmapCleanup;
                FtInterfaceCameraProxy.ImageBitmapCleanup += FtInterfaceCameraProxyOnImageBitmapCleanup;

                FtInterfaceCameraProxy.ImageBitmapInitialized -= FtInterfaceCameraProxyOnImageBitmapInitialized;
                FtInterfaceCameraProxy.ImageBitmapInitialized += FtInterfaceCameraProxyOnImageBitmapInitialized;
            }
        }

        private void UnhookEvents()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceCameraProxy.CameraFrameDecoded -= FtInterfaceCameraProxyOnCameraFrameDecoded;
                FtInterfaceCameraProxy.ImageBitmapCleanup -= FtInterfaceCameraProxyOnImageBitmapCleanup;
                FtInterfaceCameraProxy.ImageBitmapInitialized -= FtInterfaceCameraProxyOnImageBitmapInitialized;
            }
        }


        private void JoystickViewOnValuesChanged(int joystickId)
        {
            var configuration = GetRelatedJoystickConfiguration(joystickId);
            JoystickView joystick = GetRelatedJoystickView(joystickId);

            float motor1 = 0;
            float motor2 = 0;

            switch (configuration.JoystickMode)
            {
                case JoystickConfiguration.JoystickModes.Single:
                    motor1 = joystick.ThumbX;
                    motor2 = joystick.ThumbY;
                    break;
                case JoystickConfiguration.JoystickModes.Syncron:
                    CalculateSyncronValues(joystick.ThumbAngle, joystick.ThumbDistance, out motor1, out motor2);
                    break;
            }

            SetMotor(configuration.MotorIndexes[0], motor1);
            SetMotor(configuration.MotorIndexes[1], motor2);
        }

        private void CalculateSyncronValues(float thumbAngle, float thumbDistance, out float value1, out float value2)
        {
            float axis1 = 0;
            float axis2 = 0;

            if (thumbAngle >= 270 && thumbAngle <= 360)
            {
                //up right
                axis1 = (float) Math.Round(Math.Sin(MathUtils.ToRadians(thumbAngle*2f - 630f))*thumbDistance*2f);
                axis2 = (float)-Math.Round(thumbDistance * 2f);
            }
            else if (thumbAngle >= 180 && thumbAngle <= 270)
            {
                //up left
                axis1 = (float) -Math.Round(thumbDistance * 2f);
                axis2 = (float)-Math.Round(Math.Sin(MathUtils.ToRadians(thumbAngle * 2f + 270f)) * thumbDistance * 2f);
            }
            else if (thumbAngle >= 90 && thumbAngle <= 180)
            {
                // down left
                axis1 = (float) Math.Round(Math.Sin(MathUtils.ToRadians(thumbAngle*2f - 90f))*thumbDistance*2f);
                axis2 = (float)Math.Round(thumbDistance * 2f);
            }
            else if (thumbAngle >= 0 && thumbAngle <= 90)
            {
                // down right
                axis1 = (float) Math.Round(thumbDistance * 2f);
                axis2 = (float)Math.Round(Math.Sin(MathUtils.ToRadians(thumbAngle * 2f - 90f)) * thumbDistance * 2f);
            }

            value1 = axis1;
            value2 = axis2;
        }
        


        private void FtInterfaceInstanceProviderOnInstanceChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            HookEvents();
        }


        private void InitializeCameraView()
        {
            Activity.RunOnUiThread(() =>
            {
                _imageViewCameraStream?.SetImageBitmap(FtInterfaceCameraProxy.ImageBitmap);
                _imageViewCameraStream?.Invalidate();
                _firstFrame = false;


                View noCameraView = View.FindViewById(Resource.Id.noCameraStateLayout);

                if (noCameraView != null)
                {
                    noCameraView.Visibility = ViewStates.Gone;
                }
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


        private void SetMotor(int motorIndex, float percentage)
        {
            var direction = percentage > 0 ? MotorDirection.Left : MotorDirection.Right;

            int value = (int) Math.Abs(percentage/100*FtInterfaceInstanceProvider.Instance.GetMaxOutputValue());

            if (value > FtInterfaceInstanceProvider.Instance.GetMaxOutputValue())
            {
                value = FtInterfaceInstanceProvider.Instance.GetMaxOutputValue();
            }

            FtInterfaceInstanceProvider.Instance.SetMotorValue(motorIndex, value, direction);
        }

        private string[] GetMotorList()
        {
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                string[] motors = new string[FtInterfaceInstanceProvider.Instance.GetMotorCount()];

                for (int i = 0; i < motors.Length; i++)
                {
                    motors[i] = $"M{i + 1}";
                }

                return motors;
            }
            return new string[0];
        }

        public void Activate()
        {
            for (int i = 0; i < FtInterfaceInstanceProvider.Instance?.GetOutputCount(); i++)
            {
                FtInterfaceInstanceProvider.Instance?.ConfigureOutputMode(i, true);
            }
        }

        public string GetTitle(Context context)
        {
            return string.Empty;
        }
    }
}