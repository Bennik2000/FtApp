using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.Help
{
    [Activity(Label = "HelpActivity")]
    public class HelpActivity : AppCompatActivity
    {
        private ListView _listViewFaq;
        private FaqListAdapter _faqListAdapter;
        private List<FaqViewModel> _questions;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityHelpLayout);

            _listViewFaq = FindViewById<ListView>(Resource.Id.listViewFaq);

            SetupToolbar();

            LoadQuestions();
            SetupListView();
        }

        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(toolbar);

            SupportActionBar.Title = Resources.GetString(Resource.String.SelectDeviceActivity_toolbarTitle);
        }

        private void SetupListView()
        {
            _listViewFaq.Divider = null;
            _listViewFaq.DividerHeight = 0;

            _faqListAdapter = new FaqListAdapter(this, _questions);
            _listViewFaq.Adapter = _faqListAdapter;
        }

        private void LoadQuestions()
        {
            _questions = new List<FaqViewModel>();

            var questions = Resources.GetStringArray(Resource.Array.HelpFaqTitles);
            var answers = Resources.GetStringArray(Resource.Array.HelpFaqMessages);

            for (int i = 0; i < questions.Length; i++)
            {
                _questions.Add(new FaqViewModel() {Question = questions[i], Answer = answers[i]});
            }
        }

        public override void Finish()
        {
            base.Finish();

            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
        }
    }
}