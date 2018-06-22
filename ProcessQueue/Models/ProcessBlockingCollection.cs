using System.Collections.Concurrent;
using System.Linq;

namespace ProcessQueue.Models
{
    public class ProcessBlockingCollection : BlockingCollection<Process>, IProcessQueue
    {
        public void Clear()
        {
            var a = this.OrderByDescending(p => p.Id);
            Process _; while (TryTake(out _)) { }
        }

        public void OrderByPriority()
        {
            this.OrderByDescending(process => process.Priority);
        }

        public bool Remove(string id)
        {
            return false;
        }

        public void Insert(int index, Process process)
        {
            Add(process);
            int i;
            for(i = 0; i < Count-1; i++ )
            {
                Add(Take());
            }
        }
    }
}