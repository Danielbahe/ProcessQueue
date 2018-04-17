using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProcessQueue.Models;

namespace ProcessQueue
{
    public class QueueManager
    {
        private IList<Process> _queue;

        private Task _worker;
        private CancellationTokenSource _cancelationToken;
        public bool NotifyErrors { get; private set; }
        public bool StopOnError { get; private set; }
        private Action<string> _errorAction;

        public QueueManager()
        {
            _cancelationToken = new CancellationTokenSource();
            _queue = new List<Process>();
        }

        public void Start()
        {
            _worker = Task.Run(QueueExecution, _cancelationToken.Token);
        }

        public void Stop()
        {
            _cancelationToken.Cancel();
            _cancelationToken.Dispose();
            _cancelationToken = null;

            _worker = null;
        }

        public void AddProcess(Process process)
        {
            _queue.Add(process);
            _queue.OrderByDescending(p => p.Priority);
        }

        public void CancelProcess(string id)
        {
            try
            {
                Stop();
                _queue.Remove(_queue.First(p => p.Id == id));
            }
            catch (Exception)
            {
                //return don't exist this process
            }
            finally
            {
                Start();
            }
        }

        public void CancelQueue()
        {
            _cancelationToken.Cancel();
            _cancelationToken.Dispose();
            _cancelationToken = null;

            _worker = null;
            _queue.Clear();
        }

        /// <summary>
        /// Configuration method to set a function to store where you want it the queue generated as a json
        /// </summary>
        /// <param name="saveQueueMethod">Method that shoud store the json queue</param>
        /// <param name="getQueueMethod">Method that shoud return the stored queue json</param>
        /// <returns>Returns instance to fluent interface</returns>
        public QueueManager GenerateBackUpQueue(Action<string> saveQueueMethod, Func<string> getQueueMethod)
        {
            //todo
            return this;
        }

        public QueueManager StopWorkerOnError()
        {
            StopOnError = true;
            return this;
        }
        
        public QueueManager NotifyOnError(Action<string> action)
        {
            _errorAction = action;
            return this;
        }

        private async Task QueueExecution()
        {
            while (true)
            {
                try
                {
                    while (true)
                    {
                        var process = _queue.FirstOrDefault();
                        if (process != null)
                        {
                            await process.Execute();
                            _queue.Remove(process);
                            if (!process.Completed) _queue.Add(process);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (NotifyErrors)
                    {
                        var message = ex.Message;
                        if (StopOnError) message = message + " - Queue Stopped";

                        _errorAction.Invoke(message);
                    }
                    if (StopOnError) Stop();
                    else
                    {
                        var process = _queue.FirstOrDefault();
                        _queue.Remove(process);
                        if (!process.Completed) _queue.Add(process);
                    }
                }
            }
        }
    }
}