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
        private readonly ShutdownHandleDouble _shutdownHandleDouble;
        private readonly DoerService _doerService;

        public ServiceHostTest()
        {
            _shutdownWatcherDouble = new WebJobShutdownWatcherDouble();
            _doerService = new DoerService();
            _shutdownHandleDouble = new ShutdownHandleDouble();
            _host = new ServiceHost(_doerService, _shutdownWatcherDouble, new ShutdownHandleDoubleFactory(_shutdownHandleDouble), default);
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

        [Fact]
        public void propagates_exceptions_thrown_in_start()
        {
            _doerService.ThrowOnStart = true;

            Action run = () => _host.RunAsync(default).Wait(100);

            run.Should().Throw<TimeZoneNotFoundException>();
        }

        [Fact]
        public void passes_provided_cancellation_token_to_start()
        {
            var cancelledCancellationToken = new CancellationToken(true);

            Action run = () => _host.RunAsync(cancelledCancellationToken).Wait(100);

            run.Should().Throw<NotFiniteNumberException>();
        }

        [Fact]
        public async Task listens_to_ShutdownHandle_ProcessExit_as_well()
        {
            _shutdownHandleDouble.ProcessExitIn(100);

            var task = _host.RunAsync(default);
            await Task.Delay(1200);

            _doerService.Started.Should().BeTrue();
            _doerService.Stopped.Should().BeTrue();
        }

        [Fact]
        public async Task listens_to_ShutdownHandle_CancelKey_as_well()
        {
            _shutdownHandleDouble.CancelKeyPressIn(100);

            var task = _host.RunAsync(default);
            await Task.Delay(1200);

            _doerService.Started.Should().BeTrue();
            _doerService.Stopped.Should().BeTrue();
        }


        public void Dispose()
        {
            _host?.Dispose();
            _shutdownHandleDouble?.Dispose();
        }

        private class DoerService : IHostedService
        {
            public bool Started { get; private set; } = false;
            public bool Stopped { get; private set; } = false;
            public bool ThrowOnStart { get; set; } = false;

            public Task StartAsync(CancellationToken cancellationToken)
            {
                if (ThrowOnStart) throw new TimeZoneNotFoundException();
                if (cancellationToken.IsCancellationRequested) throw new NotFiniteNumberException();

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
