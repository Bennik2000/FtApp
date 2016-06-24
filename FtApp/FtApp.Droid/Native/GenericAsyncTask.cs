using Android.OS;
using System;

namespace FtApp.Droid.Native
{
    /// <summary>
    /// With this generic implementation it is easier to run a task on an async task
    /// </summary>
    class GenericAsyncTask : AsyncTask<Action, object, object>
    {
        /// <summary>
        /// This event is fired when the execution finished
        /// </summary>
        public event ExecutionFinishedEventHandler ExecutionFinished;
        public delegate void ExecutionFinishedEventHandler(object sender, EventArgs eventArgs);

        /// <summary>
        /// This event is fired when the execution was cancelled
        /// </summary>
        public event ExecutionCancelledEventHandler ExecutionCancelled;
        public delegate void ExecutionCancelledEventHandler(object sender, EventArgs eventArgs);

        protected override object RunInBackground(params Action[] @params)
        {
            foreach (Action action in @params)
            {
                try
                {
                    action.Invoke();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch(Exception) { }

                if (IsCancelled)
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        protected override void OnCancelled()
        {
            base.OnCancelled();

            ExecutionCancelled?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPostExecute(object result)
        {
            base.OnPostExecute(result);

            ExecutionFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}