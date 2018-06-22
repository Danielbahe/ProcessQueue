using System.Threading.Tasks;

namespace ProcessQueue
{
    public class BaseProcessable : IProcessable
    {
        public string Id { get; set; }
        public virtual Task<bool> Execute()
        {
            return Task.FromResult(true);
        }
    }
}