using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessQueue.Models
{
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
                Remove(this.First(p => p.Id == id));
                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }

        public void Insert(int index, Process process)
        {
            base.Insert(index, process);
        }

        public Process Take()
        {
            var process = this.FirstOrDefault();
            this.Remove(process);//todo test
            var a = this.OrderByDescending(p => p.Id);

            return process;
        }
    }
}