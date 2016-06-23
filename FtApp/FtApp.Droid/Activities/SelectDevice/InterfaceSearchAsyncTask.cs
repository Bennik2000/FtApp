using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FtApp.Droid.Activities.SelectDevice
{
    class InterfaceSearchAsyncTask : AsyncTask<object, InterfaceViewModel, object>
    {
        private Context Context { get; set; }
        public InterfaceSearcher InterfaceSearcher { get; private set; }


        internal delegate void ProgressUpdatedEventHandler(object sender, ProgressUpdatedEventArgs eventArgs);
        internal event ProgressUpdatedEventHandler ProgressUpdated;
            
        internal delegate void SearchFinishedEventHandler(object sender, SearchFinishedEventArgs eventArgs);
        internal event SearchFinishedEventHandler SearchFinished;


        private readonly IList<InterfaceViewModel> _foundInterfaces;

        public InterfaceSearchAsyncTask(Context context)
        {
            Context = context;
            InterfaceSearcher = new InterfaceSearcher(context);
            _foundInterfaces = new List<InterfaceViewModel>();
        }

        public void CancelSearch()
        {
            InterfaceSearcher?.CancelSearchForInterfaces();
        }

        protected override object RunInBackground(params object[] @params)
        {
            InterfaceSearcher = new InterfaceSearcher(Context);

            InterfaceSearcher.SearchStarted += InterfaceSearcherOnSearchStarted;
            InterfaceSearcher.SearchFinished += InterfaceSearcherOnSearchFinished;
            InterfaceSearcher.InterfaceFound += InterfaceSearcherOnInterfaceFound;

            InterfaceSearcher.SearchForInterfaces();

            InterfaceSearcher.WaitForSearchFinished();

            return _foundInterfaces.ToArray();
        }
        

        protected override void OnProgressUpdate(params InterfaceViewModel[] values)
        {
            base.OnProgressUpdate(values);

            foreach (InterfaceViewModel interfaceViewModel in values)
            {
                ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(interfaceViewModel));
            }
        }

        protected override void OnPostExecute(object result)
        {
            base.OnPostExecute(result);

            SearchFinished?.Invoke(this, new SearchFinishedEventArgs((InterfaceViewModel[])result));
        }

        private void InterfaceSearcherOnInterfaceFound(object sender, InterfaceSearcher.InterfaceFoundEventArgs eventArgs)
        {
            var controller = new InterfaceViewModel
            {
                Address = eventArgs.address,
                Name = eventArgs.Name,
                ControllerType = eventArgs.ControllerType,
                ControllerNameLaoding = false
            };

            _foundInterfaces.Add(controller);

            PublishProgress(controller);
        }

        private void InterfaceSearcherOnSearchFinished(object sender, EventArgs eventargs)
        {
        }

        private void InterfaceSearcherOnSearchStarted(object sender, EventArgs eventargs)
        {
        }


        public class ProgressUpdatedEventArgs
        {
            public InterfaceViewModel Interface { get; private set; }

            public ProgressUpdatedEventArgs(InterfaceViewModel interfaceViewModel)
            {
                Interface = interfaceViewModel;
            }
        }

        public class SearchFinishedEventArgs
        {
            public InterfaceViewModel[] Interfaces { get; private set; }

            public SearchFinishedEventArgs(InterfaceViewModel[] interfaceViewModels)
            {
                Interfaces = interfaceViewModels;
            }
        }
    }
}