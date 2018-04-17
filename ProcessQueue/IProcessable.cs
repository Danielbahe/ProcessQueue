using System.Threading.Tasks;

namespace ProcessQueue
{
    public interface IProcessable
    {
        Task<bool> Execute();
    }
}