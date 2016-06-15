using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using FtApp.Droid.Views;
using FtApp.Utils;
using System;
using TXTCommunication.Fischertechnik;

namespace FtApp.Droid.Activities.ControllInterface
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class JoystickFragment : Fragment, IFtInterfaceFragment
    {
        private JoystickView _joystickViewLeft;
        private JoystickView _joystickViewRight;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FragmentJoystick, container, false);

            view.Touch += (sender, args) => { args.Handled = true; };

            _joystickViewLeft = view.FindViewById<JoystickView>(Resource.Id.joystickLeft);
            _joystickViewRight = view.FindViewById<JoystickView>(Resource.Id.joystickRight);
            
            _joystickViewLeft.ValuesChanged += JoystickViewLeftOnValuesChanged;
            _joystickViewRight.ValuesChanged += JoystickViewRightOnValuesChanged;

            return view;
        }


        private void JoystickViewLeftOnValuesChanged(object sender, EventArgs eventArgs)
        {
            SetMotor(0, _joystickViewLeft.ThumbX);
            SetMotor(1, _joystickViewLeft.ThumbY);
        }

        private void JoystickViewRightOnValuesChanged(object sender, EventArgs eventArgs)
        {
            var angle = _joystickViewRight.ThumbAngle;
            var distance = _joystickViewRight.ThumbDistance;

            float motor1 = 0;
            float motor2 = 0;

            if (angle >= 270 && angle <= 360)
            {
                //up right
                motor1 = (float) Math.Round(Math.Sin(MathUtils.ToRadians(angle*2f - 630f))*distance*2f);
                motor2 = (float) -Math.Round(distance);
            }
            else if (angle >= 180 && angle <= 270)
            {
                //up left
                motor1 = (float) -Math.Round(distance);
                motor2 = (float) -Math.Round(Math.Sin(MathUtils.ToRadians(angle*2f + 270f))*distance*2f);
            }
            else if (angle >= 90 && angle <= 180)
            {
                // down left
                motor1 = (float) Math.Round(Math.Sin(MathUtils.ToRadians(angle*2f - 90f))*distance*2f);
                motor2 = (float) Math.Round(distance);
            }
            else if (angle >= 0 && angle <= 90)
            {
                // down right
                motor1 = (float) Math.Round(distance);
                motor2 = (float) Math.Round(Math.Sin(MathUtils.ToRadians(angle*2f - 90f))*distance*2f);
            }

            SetMotor(0, motor1);
            SetMotor(1, motor2);
        }

        private void SetMotor(int motorIndex, float percentage)
        {
            var direction = percentage > 0 ? MotorDirection.Left : MotorDirection.Right;

            int value = (int) Math.Abs(percentage/100* FtInterfaceInstanceProvider.Instance.GetMaxOutputValue());

            if (value > FtInterfaceInstanceProvider.Instance.GetMaxOutputValue())
            {
                value = FtInterfaceInstanceProvider.Instance.GetMaxOutputValue();
            }

            FtInterfaceInstanceProvider.Instance.SetMotorValue(motorIndex, value, direction);
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