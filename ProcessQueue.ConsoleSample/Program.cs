using System;
using System.Threading.Tasks;

namespace ProcessQueue.ConsoleSample
{
    class Program
    {
        static void Main(string[] args)
        {
            //var queue = new QueueExecutor();
            var queue = new QueueAgregattorTest();
            queue.Initialize().Wait();
        }
    }
}
