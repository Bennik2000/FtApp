using System;
using System.Collections.Generic;
using System.Threading;

namespace TXTCommunication.Utils
{
    /// <summary>
    /// Die TaskQueue führt eine übergebene Aufgabe auf einem anderen Thread auf. Die Aufgaben werden nacheinander auf dem selben Thread ausgeführt
    /// </summary>
    public class TaskQueue : IDisposable
    {
        private static readonly List<TaskQueue> TaskQueues = new List<TaskQueue>();

        public static void DisposeAllQueues()
        {
            foreach (TaskQueue taskQueue in TaskQueues)
            {
                taskQueue.Dispose();
            }
        }
        
        private readonly EventWaitHandle _readyEventWaitHandle;
        private readonly EventWaitHandle _workEventWaitHandle;
        private readonly EventWaitHandle _finishedEventWaitHandle;
        private readonly EventWaitHandle _workAvaiableEventWaitHandle;
        private readonly SemaphoreSlim _semaphore;
        private bool _working;

        private readonly Thread _workingThread;
        private readonly Thread _executorThread;
        private readonly Queue<Action> _workToBeDone;

        private readonly object _functionLock = new object();
        private Action _functionObject;


        public TaskQueue(string threadName) : this(ThreadPriority.AboveNormal, threadName, false)
        {

        }

        public TaskQueue(string threadName, bool isStaThread) : this(ThreadPriority.AboveNormal, threadName, isStaThread)
        {

        }

        public TaskQueue(ThreadPriority priority, string threadName, bool isStaThread)
        {
            _semaphore = new SemaphoreSlim(1);
            _workEventWaitHandle = new AutoResetEvent(false);
            _readyEventWaitHandle = new AutoResetEvent(true);
            _finishedEventWaitHandle = new AutoResetEvent(false);
            _workAvaiableEventWaitHandle = new AutoResetEvent(false);
            _workToBeDone = new Queue<Action>();

            _working = true;

            _workingThread = new Thread(WorkingMethod);
            _executorThread = new Thread(AsyncExecutor);

            if (isStaThread)
            {
                _workingThread.TrySetApartmentState(ApartmentState.STA);
                _executorThread.TrySetApartmentState(ApartmentState.STA);
            }
            else
            {
                _workingThread.TrySetApartmentState(ApartmentState.MTA);
                _executorThread.TrySetApartmentState(ApartmentState.MTA);
            }

            if (threadName != null)
            {
                _workingThread.Name = $"{threadName} Worker";
                _executorThread.Name = $"{threadName} Executor";
            }


            try
            {
                _workingThread.Priority = priority;
                _workingThread.Start();
                _executorThread.Start();
            }
            catch (ThreadStateException)
            {
                //_log.Error($"Konnte den Aufgaben Thread nicht starten: {e.Message}");
            }
            catch (OutOfMemoryException)
            {
                //_log.Error("Konnte den Aufgaben Thread nicht starten, da zu wenig Arbeitsspeicher vorhanden ist");
            }
            TaskQueues.Add(this);
        }

        /// <summary>
        /// Reiht eine neue Aufgabe in die Aufgabenliste ein
        /// </summary>
        /// <param name="work">Die zu erledigende Aufgabe</param>
        /// <param name="blocking">Der Aufruf wird solange blockiert, bis die aufgabe erledigt ist</param>
        public void DoWorkInQueue(Action work, bool blocking)
        {
            if (!_working)
            {
                throw new InvalidOperationException("Konnte die Aufgabe nicht ausführen: Die TaskQueue ist nicht gestartet");
            }

            if (Thread.CurrentThread.ManagedThreadId == _workingThread.ManagedThreadId)
            {
                try
                {
                    work();
                }
                    // ReSharper disable once CatchAllClause
                catch (Exception)
                {
                    //_log.Error($"Konnte die Aufgabe nicht ausführen: {e.Message}");
                }
                return;
            }

            if (blocking)
            {
                try
                {
                    _semaphore.Wait();
                    _readyEventWaitHandle.WaitOne(); // Warten bis fertig

                    lock (_functionLock)
                    {
                        _functionObject = work;
                    }

                    _workEventWaitHandle.Set(); // Aufgabe starten

                    _finishedEventWaitHandle.WaitOne(); // Warten bis fertig

                    _semaphore.Release();
                }
                    // ReSharper disable once CatchAllClause
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        _semaphore.Release();
                    }
                    //_log.Error($"Konnte die Aufgabe nicht erledigen: {e.Message}");
                }
            }
            else
            {
                try
                {
                    _workToBeDone.Enqueue(work);
                    _workAvaiableEventWaitHandle.Set();
                }
                    // ReSharper disable once CatchAllClause
                catch (Exception)
                {
                    //_log.Error($"Konnte die Aufgabe nicht einreihen: {e.Message}");
                }
            }
        }

        private void AsyncExecutor()
        {
            while (_working)
            {
                try
                {
                    _workAvaiableEventWaitHandle.WaitOne();
                    while (_workToBeDone.Count > 0 && _working)
                    {
                        DoWorkInQueue(_workToBeDone.Dequeue(), true);
                    }
                }
                catch (ThreadAbortException)
                {
                }
            }
        }

        private void WorkingMethod()
        {
            while (_working)
            {
                try
                {
                    _readyEventWaitHandle.Set(); // Fertig setzen
                    _workEventWaitHandle.WaitOne(); // Warten auf Aufgabe


                    if (_working)
                    {
                        lock (_functionLock)
                        {
                            try
                            {
                                _functionObject(); // Aufgabe erledigen
                            }
                                // ReSharper disable once CatchAllClause
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.StackTrace);
                                //_log.Error($"Konnte die Aufgabe nicht ausführen: {e.Message}");
                            }
                        }
                    }
                    _finishedEventWaitHandle.Set();
                }
                catch (ThreadAbortException)
                {
                }
                catch (ArgumentNullException)
                {
                    //_log.Error($"Unbekannter Fehler, der WORKER Thread ist abgestürzt: {e.Message}");
                }
            }
        }

        public void Dispose()
        {
            _working = false;

            try
            {
                _workEventWaitHandle.Set();
                _workAvaiableEventWaitHandle.Set();
            }
            catch(ObjectDisposedException) { }

            try
            {
                _workingThread.Abort();
                _executorThread.Abort();

                _readyEventWaitHandle.Close();
                _workEventWaitHandle.Close();
                _finishedEventWaitHandle.Close();
                _workAvaiableEventWaitHandle.Close();
                _semaphore.Dispose();
            }
                // ReSharper disable once CatchAllClause
            catch (Exception)
            {
                //_log.Error($"Konnte den Thread nicht beenden: {e.Message}");
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}
