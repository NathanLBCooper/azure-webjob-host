using System.Threading;

namespace AzureWebjobHost
{
    public class JobHostBuilder
    {
        private CancellationToken _externalToken = default;

        public JobHostBuilder SetExternalCancellation(CancellationToken token)
        {
            _externalToken = token;
            return this;
        }

        public JobHost Build()
        {
            return new JobHost(
                new WebJobsShutdownWatcher(), new ShutdownHandleFactory(), _externalToken);
        }
    }
}
