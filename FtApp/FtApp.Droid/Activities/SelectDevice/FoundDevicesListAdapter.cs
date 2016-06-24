using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using FtApp.Fischertechnik;

namespace FtApp.Droid.Activities.SelectDevice
{
    class FoundDevicesListAdapter : BaseAdapter<InterfaceViewModel>
    {
        private readonly List<InterfaceViewModel> _items;
        private readonly Activity _context;

        public FoundDevicesListAdapter(Activity context, List<InterfaceViewModel> items)
        {
            _context = context;
            _items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override InterfaceViewModel this[int position] => _items[position];

        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            InterfaceViewModel item = _items[position];

            View view = convertView;

            if (view == null) // no view to re-use, create new
            {
                view = _context.LayoutInflater.Inflate(Resource.Layout.ListViewItemSelectDeviceLayout, null);
            }

            var imageViewControllerIcon = view.FindViewById<ImageView>(Resource.Id.imageViewContollerIcon);
            var textViewContollerName = view.FindViewById<TextView>(Resource.Id.textViewContollerName);
            var textViewContolleraddress = view.FindViewById<TextView>(Resource.Id.textViewContolleraddress);
            var progressBarNameLoading = view.FindViewById<ProgressBar>(Resource.Id.progressBarNameLoading);

            switch (item.ControllerType)
            {
                case ControllerType.Tx:
                    imageViewControllerIcon.SetImageResource(Resource.Drawable.TxIcon);
                    break;
                case ControllerType.Txt:
                    imageViewControllerIcon.SetImageResource(Resource.Drawable.TxtIcon);
                    break;
                default:
                    imageViewControllerIcon.SetImageResource(Resource.Drawable.InterfaceUnknownIcon);
                    break;
            }

            textViewContollerName.Text = item.Name;
            textViewContolleraddress.Text = item.Address;

            progressBarNameLoading.Visibility = item.ControllerNameLaoding ? ViewStates.Visible : ViewStates.Invisible;

            return view;
        }
    }
}