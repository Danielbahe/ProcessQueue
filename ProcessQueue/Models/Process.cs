using System;
using System.Threading.Tasks;

namespace ProcessQueue.Models
{
    public class Process
    {
        public int Priority { get; private set; }
        public DateTime RequestDate { get; private set; }
        public string Id { get; set; }
        private IProcessable _processable;
        public bool Completed { get; set; }

        public Process(IProcessable processable, int priority = 0)
        {
            _processable = processable;
            RequestDate = DateTime.Now;
            Priority = priority;
        }

        public async Task Execute()
        {
            Completed = await _processable.Execute();
        }
    }
}