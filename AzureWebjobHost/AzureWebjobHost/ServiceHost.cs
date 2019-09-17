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
        private readonly ILogger<ServiceHost> _logger;

        public ServiceHost(IHostedService service, ILogger<ServiceHost> logger, CancellationToken externalToken = default)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jobHost = new JobHost(externalToken);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            async Task Serve(CancellationToken token)
            {
                _logger.LogInformation("Host starting");
                await _service.StartAsync(cancellationToken);

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                _logger.LogInformation("Host stopping");
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