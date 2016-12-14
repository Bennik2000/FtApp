using Android.Animation;
using Android.Views;
using System;

namespace FtApp.Droid.Activities.SelectDevice
{
    class HideOnFinishedAnimationListener : Java.Lang.Object, Animator.IAnimatorListener
    {
        private readonly View _animatedView;

        public HideOnFinishedAnimationListener(View view)
        {
            _animatedView = view;
        }
        
        public void OnAnimationCancel(Animator animation)
        {
        }

        public void OnAnimationEnd(Animator animation)
        {
            _animatedView.Visibility = ViewStates.Gone;
        }

        public void OnAnimationRepeat(Animator animation)
        {
        }

        public void OnAnimationStart(Animator animation)
        {
        }
    }
}