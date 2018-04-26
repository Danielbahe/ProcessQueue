using System.Collections.Generic;
using ProcessQueue.Models;

namespace ProcessQueue
{
    public class SampleService
    {
        public void RunQueue()
        {
            //basic setup
            var basicQueueManager = new QueueManager();
            basicQueueManager.Start();

            basicQueueManager
                .AddProcess(new SampleProcessable())
                .Stop()
                .Start()
                .CancelQueue()
                .AddProcess(new SampleProcessable())
                .Start();
            
            //complex setup

            var queueManager = new QueueManager();
            queueManager.StopWorkerOnError()
                .NotifyOnError(s => PublishErrorEvent())
                .GenerateBackUpQueue(SaveJsonOnDb, GetJsonFromDb)
                .DisablePriority()
                .EnablePriority()
                .AddProcess(new SampleProcessable())
                .AddProcess(new SampleProcessable())
                .Start()
                .Stop()
                .Start()
                .CancelQueue()
                .Start();

            // Add a process
            queueManager.AddProcess(new SampleProcessable());

            // Add a process list
            // useful if you want to execute it a block on certain order
            // yes, you can do it manually, but this option lets you have a cleaner code
            var processList = new List<IProcessable>();
            processList.Add(new SampleProcessable());
            processList.Add(new SampleProcessable());
            processList.Add(new SampleProcessable());
            queueManager.AddProcess(new SampleProcessable());

            // You can get queue information
            var isEnabled = queueManager.PriorityEnabled;
            var currentStatus = queueManager.Status;
            var notifyErrors = queueManager.NotifyErrors;
            var stopIfError = queueManager.StopOnError;
        }

        private void PublishErrorEvent()
        {
            //  log or raise your desired event
        }

        private void SaveJsonOnDb(string json)
        {
            //Save the json where you want
        }

        private string GetJsonFromDb()
        {
            //Get the json data you stored before
            return "json";
        }
    }
}