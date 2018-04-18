using System.Threading.Tasks;

namespace ProcessQueue
{
    internal class SampleProcessable : IProcessable
    {
        public SampleProcessable()
        {
            // prepare stuff if needed, for example inject services
        }

        public async Task<bool> Execute()
        {
            // retrieve data stored before if needed
            await RetrieveData();

            // do stuff
            DoSomeStuff();
            DoSomeOtherStuff();
            return true;
        }

        public async Task Prepare()
        {
            // Not mandatory method
            // async stuff that can't be done on ctor
            await Task.FromResult(true);
        }

        private async Task RetrieveData()
        {
            //acces to db, files...
            await Task.FromResult(true);
        }
        private void DoSomeStuff()
        {
            //easy to read
        }
        private void DoSomeOtherStuff()
        {
            //easy to read
        }
    }
}