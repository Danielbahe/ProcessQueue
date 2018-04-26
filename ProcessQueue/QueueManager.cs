using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProcessQueue.Models;

namespace ProcessQueue
{
    public class QueueManager
    {
        private string _queueType;
        private IProcessQueue _queue;
        public string Status { get; private set; }
        private Task _worker;
        private CancellationTokenSource _cancelationToken;
        public bool NotifyErrors { get; private set; }
        public bool StopOnError { get; private set; }
        public bool PriorityEnabled { get; private set; }
        private Action<string> _errorAction;
        private Action<string> _saveQueueMethod;
        private Func<string> _getQueueMethod;

        public QueueManager()
        {
            Status = "Inactive";
            _queue = new ProcessBlockingCollection();
            _queueType = "BlockingCollection";
        }

        public QueueManager Start()
        {
            if (Status == "Running") return this;

            if (_getQueueMethod != null) GetStoredQueue();

            _cancelationToken = new CancellationTokenSource();
            _worker = Task.Run(QueueExecution, _cancelationToken.Token);
            Status = "Running";

            return this;
        }
        /// <summary>
        /// Stop the queue and set Status "Stopped". The queue elements are not deleted and you can continue using Start
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager Stop()
        {
            if (Status == "Stopped" || Status == "Inactive") return this;
            _cancelationToken.Cancel();
            _cancelationToken.Dispose();
            _cancelationToken = null;

            _worker = null;
            Status = "Stopped";

            return this;
        }

        public QueueManager AddProcess(IProcessable processable)
        {
            var process = new Process(processable);
            _queue.Add(process);
            if (PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
            return this;
        }

        public QueueManager AddProcess(IEnumerable<IProcessable> processableList)
        {
            var process = new Process(processableList);
            _queue.Add(process);
            if (PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
            return this;
        }

        /// <summary>
        /// Remove from the queue the Process
        /// </summary>
        /// <param name="id">Id of the Process to be canceled</param>
        /// <returns>Returns true if works propertly</returns>
        public bool CancelProcess(string id)
        {
            var result = _queue.Remove(id);
            return result;
        }
        /// <summary>
        /// Stops and clear the queue. Sets status "Inactive"
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager CancelQueue()
        {
            Stop();
            _queue.Clear();
            Status = "Inactive";
            return this;
        }

        /// <summary>
        /// Configuration method to set a function to store where you want it the queue generated as a json
        /// </summary>
        /// <param name="saveQueueMethod">Method that shoud store the json queue</param>
        /// <param name="getQueueMethod">Method that shoud return the stored queue json</param>
        /// <returns>Returns instance to fluent interface</returns>
        public QueueManager GenerateBackUpQueue(Action<string> saveQueueMethod, Func<string> getQueueMethod)
        {
            if (saveQueueMethod == null || getQueueMethod == null) throw new NullReferenceException("Queue error, backup save actions not provided");
            _saveQueueMethod = saveQueueMethod;
            _getQueueMethod = getQueueMethod;
            return this;
        }
        /// <summary>
        /// Enable the option to stop de queue when Exception is catched. Don't clear the queue and you can continue if you call Start again.
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager StopWorkerOnError()
        {
            StopOnError = true;
            return this;
        }
        /// <summary>
        /// Execute the action when an Exception is catched, this don't break or stops the queue.
        /// </summary>
        /// <param name="action">Action invoked when exception is catched</param>
        /// <returns>Context</returns>
        public QueueManager NotifyOnError(Action<string> action)
        {
            _errorAction = action;
            NotifyErrors = true;
            return this;
        }

        public QueueManager EnablePriority()
        {
            PriorityEnabled = true;
            return this;
        }

        public QueueManager DisablePriority()
        {
            PriorityEnabled = false;
            return this;
        }
        /// <summary>
        /// Use a BlockingCollection as a queue, this is the default queue not necessary if you don't call UseListQueue before
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager UseBlockingCollectionQueue()
        {
            bool running = Status == "Running";

            Stop();
            var queue = new ProcessBlockingCollection();
            foreach (var process in _queue)
            {
                queue.Add((Process)process);
            }
            _queue = queue;
            _queueType = "BlockingCollection";

            if (running) Start();
            return this;
        }
        /// <summary>
        /// Set the queue as a List type instead of BlockingCollection
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager UseListQueue()
        {
            bool running = Status == "Running";

            Stop();
            var queue = new ProcessList();
            foreach (var process in _queue)
            {
                queue.Add((Process)process);
            }
            _queue = queue;

            _queueType = "List";
            Console.WriteLine("Queue Changed to list");
            if (running) Start();
            return this;
        }

        private async Task QueueExecution()
        {
            while (true)
            {
                Process currentProcess = null;
                try
                {
                    while (true)
                    {
                        currentProcess = _queue.Take();
                        if (currentProcess != null)
                        {
                            await currentProcess.Execute();
                            if (!currentProcess.Completed) ReinsertProcess(currentProcess);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Queue message, operation canceled");
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error on queue, Executing error, " + ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    ManageErrors(ex, currentProcess);
                }
            }
        }

        private void ManageErrors(Exception ex, Process currentProcess)
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
                if (!currentProcess.Completed) ReinsertProcess(currentProcess);
            }
        }

        private void SaveQueue()
        {
            try
            {
                if (_saveQueueMethod == null)
                    throw new NullReferenceException("Error on queue, not provided save queue action");

                var jsonQueue = JsonConvert.SerializeObject(_queue);

                _saveQueueMethod.Invoke(jsonQueue);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on queue, save queue error, " + ex.Message);
                throw;
            }
        }
        private void ReinsertProcess(Process process)
        {
            _queue.Add(process);
            if (PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
        }

        private void GetStoredQueue()
        {
            try
            {
                var jsonQueue = _getQueueMethod.Invoke();

                var queue = JsonConvert.DeserializeObject<IProcessQueue>(jsonQueue);
                if (_queueType == "List")
                {
                    _queue = new ProcessList();
                    foreach (var process in queue)
                    {
                        _queue.Add((Process)process);
                    }
                }
                else
                {
                    _queue = new ProcessBlockingCollection();
                    foreach (var process in queue)
                    {
                        _queue.Add((Process)process);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on queue, " + ex.Message);
                throw;
            }
            finally
            {
                Status = "Inactive";
            }
        }
    }
}