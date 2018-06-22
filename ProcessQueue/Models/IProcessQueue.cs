using System.Collections;

namespace ProcessQueue.Models
{
    public interface IProcessQueue : ICollection
    {
        Process Take();
        void Clear();
        void OrderByPriority();
        void Add(Process process);
        bool Remove(string id);
        void Insert(int index, Process process);

    }
}