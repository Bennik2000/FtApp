using Android.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Text.Method;

namespace FtApp.Droid.Activities.Help
{
    class FaqListAdapter : BaseAdapter<FaqViewModel>
    {
        private readonly List<FaqViewModel> _items;
        private readonly Activity _context;

        public FaqListAdapter(Activity context, List<FaqViewModel> items)
        {
            _context = context;
            _items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override FaqViewModel this[int position] => _items[position];

        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            FaqViewModel item = _items[position];

            View view = convertView;

            if (view == null) // no view to re-use, create new
            {
                view = _context.LayoutInflater.Inflate(Resource.Layout.ListViewItemFaqLayout, null);
            }

            var textViewQuestion = view.FindViewById<TextView>(Resource.Id.textViewFaqQuestion);
            var textViewAnswer = view.FindViewById<TextView>(Resource.Id.textViewFaqAnswer);


            textViewAnswer.MovementMethod = LinkMovementMethod.Instance;

            textViewQuestion.Text = item.Question;
            textViewAnswer.Text = item.Answer;

            return view;
        }
    }
}