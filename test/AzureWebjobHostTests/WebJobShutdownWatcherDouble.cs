using AzureWebjobHost;
using System.Threading;
using System.Threading.Tasks;

namespace AzureWebjobHostTests
{
    internal class WebJobShutdownWatcherDouble : IWebJobsShutdownWatcher
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public CancellationToken Token => _cts.Token;

        public void Cancel() { _cts.Cancel(); }

        public void CancelIn(int delayMs)
        {
            Task.Delay(delayMs).ContinueWith(t => _cts.Cancel());
        }

        public void Dispose()
        {
        }
    }
}
