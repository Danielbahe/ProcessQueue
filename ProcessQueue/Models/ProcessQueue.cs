using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ProcessQueue.Models
{
    public interface IProcessQueue : ICollection
    {
        Process Take();
        void Clear();
        void OrderByPriority();
        void Add(Process process);
        bool Remove(string id);

    }

    public class ProcessList : List<Process>, IProcessQueue
    {
        public void OrderByPriority()
        {
            this.OrderByDescending(process => process.Priority);
        }

        public bool Remove(string id)
        {
            try
            {
                this.Remove(this.First(p => p.Id == id));
                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }

        public Process Take()
        {
            var process = this.FirstOrDefault();
            this.Remove(process);//todo test
            var a = this.OrderByDescending(p => p.Id);

            return process;
        }
    }

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
    }
}