using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProcessQueue.Models
{
    public class BaseProcessQueue : IProcessQueue
    {
        public string QueueType { get; set; }
        public ProcessBlockingCollection ProcessBlockingCollection { get; set; }
        public ProcessList ProcessList { get; set; }

        public BaseProcessQueue(string type)
        {
            QueueType = type;
            if(QueueType == "BlockingCollection")
            {
                ProcessBlockingCollection = new ProcessBlockingCollection();
            }
            else
            {
                ProcessList = new ProcessList();
            }
        }

        public List<Process> GetProcessList()
        {
            if (QueueType == "BlockingCollection")
            {
                return ProcessBlockingCollection.ToList();
            }
            else
            {
                return ProcessList;
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (QueueType == "BlockingCollection")
            {
                return ProcessBlockingCollection.GetConsumingEnumerable().GetEnumerator();
            }
            else
            {
                return ProcessList.GetEnumerator();
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count { get; }
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
        public Process Take()
        {
            if (QueueType == "BlockingCollection")
            {
                return ProcessBlockingCollection.Take();
            }
            else
            {
                return ProcessList.Take();
            }
        }

        public void Clear()
        {
            if (QueueType == "BlockingCollection")
            {
                ProcessBlockingCollection.Clear();
            }
            else
            {
                ProcessList.Clear();
            }
        }

        public void OrderByPriority()
        {
            if (QueueType == "BlockingCollection")
            {
                ProcessBlockingCollection.OrderByPriority();
            }
            else
            {
                ProcessList.OrderByPriority();
            }
        }

        public void Add(Process process)
        {
            if (QueueType == "BlockingCollection")
            {
                ProcessBlockingCollection.Add(process);
            }
            else
            {
                ProcessList.Add(process);
            }
        }

        public bool Remove(string id)
        {
            if (QueueType == "BlockingCollection")
            {
               return  ProcessBlockingCollection.Remove(id);
            }
            else
            {
                return ProcessList.Remove(id);
            }
        }

        public void Insert(int index, Process process)
        {
            if (QueueType == "BlockingCollection")
            {
                ProcessBlockingCollection.Insert(index, process);
            }
            else
            {
                ProcessList.Insert(index, process);
            }
        }
    }
}