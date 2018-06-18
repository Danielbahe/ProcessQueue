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
        public QueueStatus Status { get; private set; }
        public bool HavePendingProcess => _queue.Count != 0;
        public bool NotifyErrors { get; private set; }
        public bool StopOnError { get; private set; }
        public bool PriorityEnabled { get; private set; }

        private CancellationTokenSource _cancelationToken;
        private Action<Exception, Process> _errorAction;
        private Action<string, string> _saveQueueMethod;
        private Func<string, string> _getQueueMethod;
        public readonly string Id;
        private readonly bool _havePendingProcess;
        private string _queueType;
        private bool _fifoMode;
        private IProcessQueue _queue;
        private Task _worker;

        public QueueManager(string id = null)
        {
            Id = id;
            Status = QueueStatus.Inactive;
            _queue = new ProcessBlockingCollection();
            _queueType = "BlockingCollection";
        }

        public QueueManager Start()
        {
            if (Status == QueueStatus.Running) return this;

            if (_getQueueMethod != null) GetStoredQueue();

            _cancelationToken = new CancellationTokenSource();
            _worker = Task.Run(QueueExecution, _cancelationToken.Token);
            Status = QueueStatus.Running;

            return this;
        }
        /// <summary>
        /// Stop the queue and set Status "Stopped". The queue elements are not deleted and you can continue using Start
        /// </summary>
        /// <returns>Context</returns>
        public QueueManager Stop()
        {
            if (Status == QueueStatus.Stopped || Status == QueueStatus.Inactive) return this;
            _cancelationToken.Cancel();
            _cancelationToken.Dispose();
            _cancelationToken = null;

            _worker = null;
            Status = QueueStatus.Stopped;

            return this;
        }

        public QueueManager AddProcess(IProcessable processable, int priority = 0, string id = null)
        {
            var process = new Process(processable);
            _queue.Add(process);
            if (PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
            return this;
        }

        public QueueManager AddProcess(IEnumerable<IProcessable> processableList, int priority = 0, string id = null)
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
            Status = QueueStatus.Inactive;
            return this;
        }

        /// <summary>
        /// Configuration method to set a function to store where you want it the queue generated as a json
        /// </summary>
        /// <param name="saveQueueMethod">Method that shoud store the json queue</param>
        /// <param name="getQueueMethod">Method that shoud return the stored queue json</param>
        /// <returns>Returns instance to fluent interface</returns>
        public QueueManager GenerateBackUpQueue(Action<string, string> saveQueueMethod, Func<string, string> getQueueMethod)
        {
            UseListQueue();
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
        public QueueManager NotifyOnError(Action<Exception, Process> action)
        {
            _errorAction = action;
            NotifyErrors = true;
            return this;
        }

        public QueueManager EnablePriority(bool enabled = true)
        {
            if (enabled) FifoMode(false);
            PriorityEnabled = enabled;
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
            bool running = Status == QueueStatus.Running;

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
            if (_queueType == "List") return this;

            bool running = Status == QueueStatus.Running;

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
        public QueueManager FifoMode(bool active)
        {
            EnablePriority(false);
            _fifoMode = active;
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

                if (_saveQueueMethod != null) SaveQueue();
            }
        }

        private void ManageErrors(Exception ex, Process currentProcess)
        {
            if (NotifyErrors)
            {
                _errorAction.Invoke(ex, currentProcess);
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

                _saveQueueMethod.Invoke(jsonQueue, Id);
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
            if (_fifoMode)
            {
                _queue.Insert(0, process);
            }
            else
            {
                _queue.Add(process);
                if (PriorityEnabled) _queue.OrderByPriority();
            }
        }

        private void GetStoredQueue()
        {
            try
            {
                var jsonQueue = _getQueueMethod.Invoke(Id);

                IProcessQueue queue = JsonConvert.DeserializeObject<ProcessList>(jsonQueue);

                    queue = JsonConvert.DeserializeObject<ProcessList>(jsonQueue);


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
                Status = QueueStatus.Inactive;
            }
        }
    }
}