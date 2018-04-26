using System;
using System.Threading.Tasks;

namespace ProcessQueue.ConsoleSample
{
    public class ConsoleWriteProcessable : IProcessable
    {
        private string _text;
        public ConsoleWriteProcessable(string text = "")
        {
            _text = text;
        }
        public async Task<bool> Execute()
        {
            Console.WriteLine(DateTime.Now + _text);
            await Task.Delay(1000);
            return true;
        }
    }
}