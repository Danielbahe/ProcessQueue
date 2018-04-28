using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessQueue.ConsoleSample
{
    public class QueueAgregattorTest
    {
        private QueueManager _queueManager;

        public QueueAgregattorTest()
        {
            _queueManager = new QueueManager();
        }

        public async Task Initialize()
        {
            _queueManager.NotifyOnError(Console.WriteLine) //I want to know if it works
                .Start();
            _queueManager.AddProcess(new ConsoleWriteProcessable())
                .AddProcess(new ConsoleWriteProcessable())
                .AddProcess(new ConsoleWriteProcessable());

            _queueManager.Stop()
                .AddProcess(new ConsoleWriteProcessable(), 1, "myId")
                .AddProcess(new ConsoleWriteProcessable(),1)
                .AddProcess(new List<IProcessable>{new ConsoleWriteProcessable()})
                .AddProcess(new BreakingProcessable())
                .AddProcess(new ConsoleWriteProcessable("process after breaking"));

            await Task.Delay(3000);
            _queueManager.UseListQueue()
                .Start();


            Console.ReadKey();
            _queueManager.AddProcess(new ConsoleWriteProcessable())
                .AddProcess(new BreakingProcessable());
            Console.ReadKey();
        }
    }
}