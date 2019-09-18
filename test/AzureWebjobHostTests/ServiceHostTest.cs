using AzureWebjobHost;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AzureWebjobHostTests
{
    public class ServiceHostTest : IDisposable
    {
        private readonly ServiceHost _host;
        private readonly WebJobShutdownWatcherDouble _shutdownWatcherDouble;
        private readonly DoerService _doerService;

        public ServiceHostTest()
        {
            _shutdownWatcherDouble = new WebJobShutdownWatcherDouble();
            _doerService = new DoerService();
            _host = new ServiceHost(_doerService, webJobsShutdownWatcher: _shutdownWatcherDouble);
        }

        [Fact]
        public async Task service_runs_while_not_cancelled()
        {
            var task = _host.RunAsync(default);

            _shutdownWatcherDouble.CancelIn(500);
            await Task.Delay(400);

            _doerService.Started.Should().BeTrue();
            _doerService.Stopped.Should().BeFalse();
        }

        [Fact]
        public async Task service_stops_when_cancelled()
        {
            var task = _host.RunAsync(default);

            _shutdownWatcherDouble.CancelIn(100);
            await Task.Delay(1100);

            _doerService.Started.Should().BeTrue();
            _doerService.Stopped.Should().BeTrue();
        }

        [Fact]
        public async Task service_never_starts_if_token_already_cancelled()
        {
            _shutdownWatcherDouble.Cancel();
            await _host.RunAsync(default);

            _doerService.Started.Should().BeFalse();
            _doerService.Stopped.Should().BeFalse();
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        private class DoerService : IHostedService
        {
            public bool Started { get; private set; } = false;
            public bool Stopped { get; private set; } = false;

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Started = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                Stopped = true;
                return Task.CompletedTask;
            }
        }
    }
}
