using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureWebjobHost
{
    /// <summary>
    /// Provide Azure Web Job cancellation wrapping for a IHostedService
    /// </summary>
    public class ServiceHost : IDisposable
    {
        private readonly JobHost _jobHost;
        private readonly IHostedService _service;

        public ServiceHost(IHostedService service, CancellationToken externalToken = default,
            IWebJobsShutdownWatcher webJobsShutdownWatcher = default)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _jobHost = new JobHost(externalToken, webJobsShutdownWatcher);
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