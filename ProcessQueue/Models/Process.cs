using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessQueue.Models
{
    public class Process
    {
        public int Priority { get; private set; }
        public DateTime RequestDate { get; private set; }
        public string Id { get; set; }
        private readonly IEnumerable<IProcessable> _processable;
        public bool Completed { get; set; }

        public Process(IProcessable processable, int priority = 0, string id = null)
        {
            var list = new List<IProcessable>();
            list.Add(processable);
            _processable = list;
            RequestDate = DateTime.Now;
            Priority = priority;
            Id = id;
        }

        public Process(IEnumerable<IProcessable> processableList, int priority = 0, string id = null)
        {
            _processable = processableList;
            RequestDate = DateTime.Now;
            Priority = priority;
            Id = id;
        }

        private Process()
        {

        }

        public async Task Execute()
        {
            var result = false;
            
            foreach (var processable in _processable)
            {
                result = await processable.Execute();
                if (!result) break;
            }
            Completed = result;
        }
    }
}