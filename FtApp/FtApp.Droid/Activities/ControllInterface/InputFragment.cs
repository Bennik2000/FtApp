using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using FtApp.Fischertechnik.Txt.Events;
using TXTCommunication.Fischertechnik;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControllInterface
{
    public class InputFragment : Fragment, IFtInterfaceFragment
    {
        private readonly List<InputViewModel> _inputViewModels;

        private IFtInterface _ftInterface;
        private ListAdapter _listAdapter;
        private ListView _listViewInputPorts;

        public InputFragment()
        {
            _inputViewModels = new List<InputViewModel>();
        }

        public override void OnAttach(Context context)
        {
            foreach (InputViewModel inputViewModel in _inputViewModels)
            {
                inputViewModel.Context = context;
            }

            base.OnAttach(context);
        }
        
        private void FtInterfaceOnOnlineStarted(object sender, EventArgs eventArgs)
        {
            _inputViewModels.Clear();

            for (int i = 0; i < _ftInterface.GetInputCount(); i++)
            {
                var inputViewModel = new InputViewModel()
                {
                    Context = Activity,
                    InputIndex = i,
                    FtInterface = _ftInterface,
                    InputValue = 1
                };

                inputViewModel.ChangeInputDevice(GetInputDeviceFromPreferences(i));

                _inputViewModels.Add(inputViewModel);
            }
            Activity?.RunOnUiThread(() => _listAdapter.NotifyDataSetChanged());
        }

        private void FtInterfaceOnInputValueChanged(object sender, InputValueChangedEventArgs inputValueChangedEventArgs)
        {
            foreach (int inputPort in inputValueChangedEventArgs.InputPorts)
            {
                _inputViewModels[inputPort].InputValue = _ftInterface.GetInputValue(inputPort);
                Activity?.RunOnUiThread(() =>
                {
                    if (_inputViewModels.Count > inputPort)
                    {
                        UpdateListView(inputPort, _inputViewModels[inputPort]);
                    }
                });
            }
        }
        
        private void FtInterfaceOnOnlineStopped(object sender, EventArgs eventArgs)
        {
            for (int i = 0; i < _inputViewModels.Count; i++)
            {
                SetInputDeviceInPreferences(i, _inputViewModels[i].InputDevice);
            }

            _inputViewModels.Clear();
            Activity?.RunOnUiThread(() => _listAdapter.NotifyDataSetChanged());
        }

        private InputDevices GetInputDeviceFromPreferences(int inputIndex)
        {
            ISharedPreferences settings = Activity.GetSharedPreferences(typeof(InputFragment).FullName, 0);

            int value = settings.GetInt($"InputState_{inputIndex}", (int) InputDevices.Switch);
            Console.WriteLine((InputDevices)value);
            return (InputDevices) value;
        }

        private void SetInputDeviceInPreferences(int inputIndex, InputDevices inputDevice)
        {
            ISharedPreferences settings = Activity.GetSharedPreferences(typeof(InputFragment).FullName, 0);
            ISharedPreferencesEditor editor = settings.Edit();


            Console.WriteLine(inputDevice);

            editor.PutInt($"InputState_{inputIndex}", (int)inputDevice);
            editor.Commit();
        }

        private void UpdateListView(int position, InputViewModel item)
        {
            int first = _listViewInputPorts.FirstVisiblePosition;
            int last = _listViewInputPorts.LastVisiblePosition;

            if (position < first || position > last)
            {
                return;
            }
            View view = _listViewInputPorts.GetChildAt(position - first);


            
            var textViewInputValue = view.FindViewById<TextView>(Resource.Id.textViewInputValue);
            var progressBarValue = view.FindViewById<ProgressBar>(Resource.Id.progressBarValue);
            
            textViewInputValue.Text = item.GetDecoratedValue();

            progressBarValue.Progress = item.InputValue;
        }

        public void SetFtInterface(IFtInterface ftInterface)
        {
            _ftInterface = ftInterface;

            _ftInterface.OnlineStarted += FtInterfaceOnOnlineStarted;
            _ftInterface.InputValueChanged += FtInterfaceOnInputValueChanged;
            _ftInterface.OnlineStopped += FtInterfaceOnOnlineStopped;
        }

        public string GetTitle(Context context)
        {
            return context.GetText(Resource.String.ControlTxtActivity_tabInputTitle);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.InputFragmentLayout, container, false);

            _listViewInputPorts = view.FindViewById<ListView>(Resource.Id.listViewInputPorts);
            _listViewInputPorts.Divider = null;
            _listViewInputPorts.DividerHeight = 0;

            _listAdapter = new ListAdapter(Activity, _inputViewModels);

            _listViewInputPorts.Adapter = _listAdapter;

            return view;
        }

        /// <summary>
        /// This class displays the input views
        /// </summary>
        private class ListAdapter : BaseAdapter<InputViewModel>
        {
            private readonly List<InputViewModel> _items;
            private readonly Dictionary<InputViewModel, PopupMenu> _popupMenus;
            private readonly Activity _context;

            public ListAdapter(Activity context, List<InputViewModel> items)
            {
                _context = context;
                _items = items;
                _popupMenus = new Dictionary<InputViewModel, PopupMenu>();
            }

            public override long GetItemId(int position)
            {
                return position;
            }

            public override InputViewModel this[int position] => _items[position];

            public override int Count => _items.Count;


            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                InputViewModel item = _items[position];

                View view = convertView;

                if (view == null) // no view to re-use, create new
                {
                    view = _context.LayoutInflater.Inflate(Resource.Layout.InputListViewItemLayout, null);

                    var imageViewContextualMenu = view.FindViewById<ImageView>(Resource.Id.imageViewContextualMenu);

                    imageViewContextualMenu.Click += delegate
                    {
                        ShowInputModeContextMenu(view.Context, imageViewContextualMenu, position);
                    };

                    FadeInAnimation(view, position);
                }


                var textViewInputIndex = view.FindViewById<TextView>(Resource.Id.textViewInputIndex);
                var textViewInputValue = view.FindViewById<TextView>(Resource.Id.textViewInputValue);

                var progressBarValue = view.FindViewById<ProgressBar>(Resource.Id.progressBarValue);

                textViewInputIndex.Text = item.GetDecoratedInputIndex();
                textViewInputValue.Text = item.GetDecoratedValue();
                    
                progressBarValue.Max = item.InputMaxValue;
                progressBarValue.Progress = item.InputValue;


                return view;
            }

            private void FadeInAnimation(View view, int position)
            {
                view.Alpha = 0;
                view.TranslationY = 20;

                view.Animate().Alpha(1).SetDuration(225).SetStartDelay(position*20).Start();
                view.Animate().TranslationY(0).SetDuration(100).SetStartDelay(position*20).Start();
            }

            private void ShowInputModeContextMenu(Context context, ImageView imageView, int position)
            {
                if (_popupMenus.ContainsKey(_items[position]))
                {
                    _popupMenus[_items[position]].Show();
                }
                else
                {
                    var popup = new PopupMenu(context, imageView);
                    popup.MenuInflater.Inflate(Resource.Menu.ConfigureInputPopupMenu, popup.Menu);
                    popup.Show();

                    popup.MenuItemClick += delegate (object sender, PopupMenu.MenuItemClickEventArgs args)
                    {
                        ConfigureInputPopupOnMenuItemClick(args, position);
                    };

                    _popupMenus.Add(_items[position], popup);
                }
            }

            private void ConfigureInputPopupOnMenuItemClick(PopupMenu.MenuItemClickEventArgs args, int position)
            {
                int id = args.Item.ItemId;

                args.Item.SetChecked(!args.Item.IsChecked);

                var item = _items[position];

                switch (id)
                {
                    case Resource.Id.menuInputModeSwitch:
                        item.ChangeInputDevice(InputDevices.Switch);
                        break;

                    case Resource.Id.menuInputModeNtc:
                        item.ChangeInputDevice(InputDevices.Ntc);
                        break;

                    case Resource.Id.menuInputModeTrailSensor:
                        item.ChangeInputDevice(InputDevices.TrailSensor);
                        break;

                    case Resource.Id.menuInputModeUltrasonic:
                        item.ChangeInputDevice(InputDevices.Ultrasonic);
                        break;

                    case Resource.Id.menuInputModeAnalogR:
                        item.ChangeInputDevice(InputDevices.AnalogR);
                        break;

                    case Resource.Id.menuInputModeDigitalR:
                        item.ChangeInputDevice(InputDevices.DigitalR);
                        break;

                    case Resource.Id.menuInputModeAnalogU:
                        item.ChangeInputDevice(InputDevices.AnalogU);
                        break;

                    case Resource.Id.menuInputModeDigitalU:
                        item.ChangeInputDevice(InputDevices.DigitalU);
                        break;
                }

                NotifyDataSetChanged();
            }
        }
        
        /// <summary>
        /// This class holds the input values
        /// </summary>
        private class InputViewModel
        {
            private int _inputValue;
            public IFtInterface FtInterface { get; set; }
            public int InputIndex { get; set; }
            
            public int InputMaxValue { get; set; }
            public string InputUnit { get; set; }
            public bool IsDigital { get; set; }
            public InputDevices InputDevice { get; set; }
            public InputMode InputMode { get; set; }
            public int InputValue
            {
                get { return _inputValue; }
                set
                {
                    _inputValue = value;
                    if (value > InputMaxValue)
                    {
                        _inputValue = InputMaxValue;
                    }
                }
            }


            public Context Context { get; set; }
            
            public string GetDecoratedValue()
            {
                switch (InputDevice)
                {
                    case InputDevices.Switch:
                        return InputValue == 1
                            ? Context.GetString(Resource.String.ControlTxtActivity_inputDigitalOne)
                            : Context.GetString(Resource.String.ControlTxtActivity_inputDigitalZero);

                    case InputDevices.Ntc:
                        return $"{NtcToCelsius(InputValue)} {InputUnit}";

                    case InputDevices.TrailSensor:
                        return InputValue == 1
                            ? Context.GetString(Resource.String.ControlTxtActivity_inputDigitalNoTrail)
                            : Context.GetString(Resource.String.ControlTxtActivity_inputDigitalTrail);
                        
                    default:
                        return $"{InputValue} {InputUnit}";
                }
            }

            public string GetDecoratedInputIndex()
            {
                return $"I{InputIndex + 1}";
            }

            public void ChangeInputDevice(InputDevices mode)
            {
                InputDevice = mode;
                switch (mode)
                {
                    case InputDevices.Switch:
                        InputMaxValue = 1;
                        IsDigital = true;
                        InputUnit = "";
                        InputMode = InputMode.ModeR;
                        break;

                    case InputDevices.Ntc:
                        InputMaxValue = 2000;
                        IsDigital = false;
                        InputUnit = "°C";
                        InputMode = InputMode.ModeR;
                        break;

                    case InputDevices.TrailSensor:
                        InputMaxValue = 1;
                        IsDigital = true;
                        InputUnit = "";
                        InputMode = InputMode.ModeU;
                        break;

                    case InputDevices.Ultrasonic:
                        InputMaxValue = 1023;
                        IsDigital = false;
                        InputUnit = "cm";
                        InputMode = InputMode.ModeUltrasonic;
                        break;



                    case InputDevices.AnalogU:
                        InputMaxValue = 9999;
                        IsDigital = false;
                        InputMode = InputMode.ModeU;
                        InputUnit = "";
                        break;
                    case InputDevices.DigitalU:
                        InputMaxValue = 1;
                        IsDigital = true;
                        InputMode = InputMode.ModeU;
                        InputUnit = "";
                        break;

                    case InputDevices.AnalogR:
                        InputMaxValue = 2000;
                        IsDigital = false;
                        InputMode = InputMode.ModeR;
                        InputUnit = "";
                        break;
                    case InputDevices.DigitalR:
                        InputMaxValue = 1;
                        IsDigital = true;
                        InputMode = InputMode.ModeR;
                        InputUnit = "";
                        break;
                }

                if (IsDigital)
                {
                    InputMaxValue = 1;
                }

                FtInterface.ConfigureInputMode(InputIndex, InputMode, IsDigital);
            }

            private int NtcToCelsius(int ntc)
            {
                var log = Math.Log(ntc);
                
                var celsius = log*log*1.39323522 + log*-43.9417405 + 271.870481;

                return (int) Math.Round(celsius);
            }
        }

        private enum InputDevices
        {
            Switch,
            Ntc,
            TrailSensor,
            Ultrasonic,
            AnalogR,
            DigitalR,
            AnalogU,
            DigitalU
        }
    }
}