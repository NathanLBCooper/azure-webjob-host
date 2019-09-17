using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzureWebjobHost
{
    /// <summary>
    /// Provide Azure Web Job cancellation wrapping for a cancellable action
    /// </summary>
    public class JobHost : IDisposable
    {
        private const int stoppingWaitTimeMs = 4000;

        private readonly IWebJobsShutdownWatcher _webJobsShutdownWatcher;
        private readonly CancellationTokenSource _internalCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _linkedCts;
        private readonly ManualResetEventSlim _waitForExit = new ManualResetEventSlim(true);

        public JobHost(CancellationToken externalToken = default, IWebJobsShutdownWatcher webJobsShutdownWatcher = default)
        {
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalCts.Token, externalToken);
            _webJobsShutdownWatcher = webJobsShutdownWatcher ?? new WebJobsShutdownWatcher();

            void Shutdown()
            {
                try
                {
                    _internalCts.Cancel();
                }
                catch (ObjectDisposedException)
                {

                }
                finally
                {
                    _waitForExit.Wait(stoppingWaitTimeMs);
                }
            }

            _webJobsShutdownWatcher.Token.Register(Shutdown);
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Shutdown();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Shutdown();
                eventArgs.Cancel = true;
            };
        }

        public async Task RunAsync(Func<CancellationToken, Task> action)
        {
            try
            {
                _waitForExit.Reset();
                await action.Invoke(_linkedCts.Token);
            }
            finally
            {
                _waitForExit.Set();
            }
        }

        public void Dispose()
        {
            _webJobsShutdownWatcher?.Dispose();
            _linkedCts?.Dispose();
            _internalCts?.Dispose();
            _waitForExit?.Dispose();
        }
    }
}
