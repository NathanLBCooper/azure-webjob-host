using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AzureWebjobHost
{
    /// <summary>
    /// Provide Azure Web Job cancellation wrapping for a IHostedService
    /// </summary>
    public class ServiceHost : IDisposable
    {
        private readonly JobHost _jobHost;
        private readonly IHostedService _service;

        public ServiceHost(IHostedService service,
            IWebJobsShutdownWatcher webJobsShutdownWatcher, IShutdownHandleFactory shutdownHandleFactory,
            CancellationToken externalToken)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _jobHost = new JobHost(webJobsShutdownWatcher, shutdownHandleFactory, externalToken);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            async Task Serve(CancellationToken token)
            {
                if (token.IsCancellationRequested) return;

                await _service.StartAsync(cancellationToken);

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                await _service.StopAsync(default);
            };

            await _jobHost.RunAsync(Serve);
        }

        public void Dispose()
        {
            _jobHost.Dispose();
        }
    }
}