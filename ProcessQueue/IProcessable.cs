using System.Threading.Tasks;

namespace ProcessQueue
{
    public interface IProcessable
    {
        string Id { get; set; }
        Task<bool> Execute();
    }
}