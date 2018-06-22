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
        public IEnumerable<BaseProcessable> Processables { get; set; }
        public bool Completed { get; set; }

        public Process(BaseProcessable processable, int priority = 0, string id = null)
        {
            var list = new List<BaseProcessable>();
            list.Add(processable);
            Processables = list;
            RequestDate = DateTime.Now;
            Priority = priority;
            Id = id;
        }

        public Process(IEnumerable<BaseProcessable> processableList, int priority = 0, string id = null)
        {
            Processables = processableList;
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

            foreach (var processable in Processables)
            {
                var miau = (Task) processable.GetType().GetMethod("Execute").Invoke(processable, null);
                await miau.ConfigureAwait(false);
                var resultProperty = miau.GetType().GetProperty("Result");
                result = (bool) resultProperty.GetValue(miau);
                if (!result) break;
            }

            Completed = result;
        }
    }
}