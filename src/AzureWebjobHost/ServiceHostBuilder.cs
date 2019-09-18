using Microsoft.Extensions.Hosting;
using System.Threading;

namespace AzureWebjobHost
{
    public class ServiceHostBuilder
    {
        private IHostedService _service = null;
        private CancellationToken _externalToken = default;

        public ServiceHostBuilder HostService(IHostedService service)
        {
            _service = service;
            return this;
        }

        public ServiceHostBuilder SetExternalCancellation(CancellationToken token)
        {
            _externalToken = token;
            return this;
        }

        public ServiceHost Build()
        {
            return new ServiceHost(
                _service, new WebJobsShutdownWatcher(), new ShutdownHandleFactory(), _externalToken);
        }
    }
}
