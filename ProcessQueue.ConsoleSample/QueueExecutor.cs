using System;
using System.Threading.Tasks;

namespace ProcessQueue.ConsoleSample
{
    public class QueueExecutor
    {
        private int _count = 0;
        private QueueManager _queueManager;
        private bool _stopped = false;
        public QueueExecutor()
        {
            _queueManager = new QueueManager();
        }

        public async Task Initialize()
        {
            _queueManager.StopWorkerOnError() //I want to know if it works
                .UseListQueue() //lets test the List type queue
                .NotifyOnError(WriteOnConsole)
                .Start();

            await QueueTest();

            Console.WriteLine("Finished");
            _queueManager.CancelQueue();
            Console.WriteLine("Canceled queue");
            //lets start again with a BlockingCollectionqueue
            Console.WriteLine("lets start again with a BlockingCollectionqueue");


            _count = 0;
            _stopped = false;
            _queueManager
                .UseBlockingCollectionQueue()
                .Start();
            await QueueTest();

            Console.WriteLine("Lets break the queue");
            await BreakQueue();

            Console.WriteLine("Disable error break");
            _queueManager = null;
            _queueManager = new QueueManager();
            Console.WriteLine("Enable error notification ('don't break the queue'");

            _queueManager
            .Start();

            await BreakQueue();
            Console.ReadKey();
        }

        private async Task QueueTest()
        {
            while (_count < 5)
            {
                await ExecuteQueue();
                if (_count == 3 && !_stopped)
                {
                    await StopQueueForSeconds(3000);
                }
            }
        }

        private async Task ExecuteQueue()
        {
            _queueManager.AddProcess(new ConsoleWriteProcessable());
            _queueManager.AddProcess(new ConsoleWriteProcessable());
            await Task.Delay(1000);
            _count++;
        }

        private async Task StopQueueForSeconds(int seconds)
        {
            _queueManager.Stop();
            Console.WriteLine("Stopped");
            await Task.Delay(seconds);
            _stopped = true;
        }

        private async Task BreakQueue()
        {

            _queueManager.AddProcess(new ConsoleWriteProcessable());
            await Task.Delay(1000);
            Console.WriteLine("added breaking process");
            _queueManager.AddProcess(new BreakingProcessable());
            _queueManager.AddProcess(new ConsoleWriteProcessable());
        }

        private void WriteOnConsole(string message)
        {
            Console.WriteLine(message + DateTime.Now);
        }
    }
}