using System;
using System.Threading.Tasks;

namespace ProcessQueue.ConsoleSample
{
    public class BreakingProcessable : IProcessable
    {
        public async Task<bool> Execute()
        {
            throw new Exception("I am throwing an error!");
        }
    }
}