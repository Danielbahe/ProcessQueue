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
        }

        public QueueManager Start()
        {
            if(Status == "Running") return this;

            if (_getQueueMethod != null) GetStoredQueue();

            Status = "Running";
            _cancelationToken = new CancellationTokenSource();
            _worker = Task.Run(QueueExecution, _cancelationToken.Token);

            return this;
        }
        public QueueManager Stop()
        {
            if (Status == "Stopped") return this;
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
            if(PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
            return this;
        }
        public QueueManager AddProcess(IEnumerable<IProcessable> processableList)
        {
            var process = new Process(processableList);
            _queue.Add(process);
            if(PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
            return this;
        }

        private void ReinsertProcess(Process process)
        {
            _queue.Add(process);
            if (PriorityEnabled) _queue.OrderByPriority();
            if (_saveQueueMethod != null) SaveQueue();
        }

        public bool CancelProcess(string id)
        {
            var result = _queue.Remove(id);
            return result;
        }
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
            _saveQueueMethod = saveQueueMethod;
            _getQueueMethod = getQueueMethod;
            return this;
        }

        public QueueManager StopWorkerOnError()
        {
            StopOnError = true;
            return this;
        }
        
        public QueueManager NotifyOnError(Action<string> action)
        {
            //todo implement on Processable too
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
        public QueueManager UseBlockingCollectionQueue()
        {
            if (Status == "Running") Stop();

            _queue = new ProcessBlockingCollection();

            if (Status != "Running") Start();
            return this;
        }

        public QueueManager UseListQueue()
        {
            var running = _worker != null;
            if(running) Stop();

            _queue = new ProcessList();

            if(running) Start();
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
                    break;
                }
                catch (InvalidOperationException)
                {
                    //todo manage Max, min and empty
                    break;
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
            //todo custom exceptions
            try
            {
                if(_saveQueueMethod == null) throw new NullReferenceException("Error on queue, not provided save queue action");

                var jsonQueue = JsonConvert.SerializeObject(_queue);

                _saveQueueMethod.Invoke(jsonQueue);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on queue, save queue error");
                throw ex;
            }
            
        }

        private void GetStoredQueue()
        {
            //todo custom exceptions
            try
            {
                if (_getQueueMethod == null)
                    throw new NullReferenceException("Error on queue, not provided get queue action");
                var jsonQueue = _getQueueMethod.Invoke();

                _queue = JsonConvert.DeserializeObject<IProcessQueue>(jsonQueue);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on queue, get queue error");
                throw ex;
            }
            finally
            {
                Status = "Inactive";
            }
        }
    }
}