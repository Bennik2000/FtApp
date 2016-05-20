using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using FtApp.Fischertechnik.Txt.Events;
using System;
using System.Collections.Generic;
using TXTCommunication.Fischertechnik;
using Fragment = Android.Support.V4.App.Fragment;

namespace FtApp.Droid.Activities.ControlTxt
{
    public class InputFragment : Fragment
    {
        private readonly FtInterface _ftInterface;

        private readonly List<InputViewModel> _inputViewModels;

        private ListAdapter _listAdapter;
        private ListView _listViewInputPorts;

        public InputFragment(FtInterface ftInterface)
        {
            _ftInterface = ftInterface;

            _ftInterface.OnlineStarted += FtInterfaceOnOnlineStarted;
            _ftInterface.InputValueChanged += FtInterfaceOnInputValueChanged;

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
            for (int i = 0; i < _ftInterface.GetInputCount(); i++)
            {
                var inputViewModel = new InputViewModel()
                {
                    InputIndex = i,
                    FtInterface = _ftInterface,
                    InputUnit = "",
                    InputMaxValue = 1,
                    InputValue = 1,
                    IsDigital = true
                };

                inputViewModel.ChangeInputDevice(InputDevices.Switch);

                _inputViewModels.Add(inputViewModel);
            }
        }

        private void FtInterfaceOnInputValueChanged(object sender, InputValueChangedEventArgs inputValueChangedEventArgs)
        {
            foreach (int inputPort in inputValueChangedEventArgs.InputPorts)
            {
                _inputViewModels[inputPort].InputValue = _ftInterface.GetInputValue(inputPort);
                Activity?.RunOnUiThread(() =>
                {
                    UpdateListView(inputPort, _inputViewModels[inputPort]);
                });
            }
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

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.InputFragmentLayout, container, false);

            _listViewInputPorts = view.FindViewById<ListView>(Resource.Id.listViewInputPorts);

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
                    case Resource.Id.menuInputModeDigitalR:
                        item.ChangeInputDevice(InputDevices.Switch);
                        break;

                    case Resource.Id.menuInputModeAnalogR2:
                        item.ChangeInputDevice(InputDevices.Ntc);
                        break;

                    case Resource.Id.menuInputModeDigitalU:
                        item.ChangeInputDevice(InputDevices.TrailSensor);
                        break;

                    case Resource.Id.menuInputModeAnalogU:
                        item.ChangeInputDevice(InputDevices.ColorSensor);
                        break;

                    case Resource.Id.menuInputModeUltrasonic:
                        item.ChangeInputDevice(InputDevices.Ultrasonic);
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
            public FtInterface FtInterface { get; set; }
            public int InputIndex { get; set; }
            
            public int InputMaxValue { get; set; }
            public string InputUnit { get; set; }
            public bool IsDigital { get; set; }
            public InputDevices InputDevice { get; set; }
            public FtInterface.InputMode InputMode { get; set; }
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

                    case InputDevices.ColorSensor:
                    case InputDevices.Ultrasonic:
                        return $"{InputValue} {InputUnit}";
                }
                return string.Empty;
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
                        InputMode = FtInterface.InputMode.ModeR;
                        break;

                    case InputDevices.Ntc:
                        InputMaxValue = 2000;
                        IsDigital = false;
                        InputUnit = "°C";
                        InputMode = FtInterface.InputMode.ModeR;
                        break;

                    case InputDevices.TrailSensor:
                        InputMaxValue = 1;
                        IsDigital = true;
                        InputUnit = "";
                        InputMode = FtInterface.InputMode.ModeU;
                        break;
                    case InputDevices.ColorSensor:
                        InputMaxValue = 9999;
                        IsDigital = false;
                        InputMode = FtInterface.InputMode.ModeU;
                        InputUnit = "";
                        break;
                    case InputDevices.Ultrasonic:
                        InputMaxValue = 1023;
                        IsDigital = false;
                        InputUnit = "cm";
                        InputMode = FtInterface.InputMode.ModeUltrasonic;
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
            ColorSensor,
            Ultrasonic
        }
    }
}