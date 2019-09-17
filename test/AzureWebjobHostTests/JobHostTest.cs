using AzureWebjobHost;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AzureWebjobHostTests
{
    // todo these tests are pretty dull and don't test the multiple things we listen for (as well as WebJobShutdownWatcher) and the 4 second grace period
    public class JobHostTest : IDisposable
    {
        private readonly JobHost _host;
        private readonly WebJobShutdownWatcherDouble _shutdownWatcherDouble;
        private readonly Doer _doer;

        public JobHostTest()
        {
            _shutdownWatcherDouble = new WebJobShutdownWatcherDouble();
            _host = new JobHost(webJobsShutdownWatcher: _shutdownWatcherDouble);
            _doer = new Doer();
        }

        [Fact]
        public async Task uncancelled_action_runs_to_completion()
        {
            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeTrue();
        }

        [Fact]
        public async Task already_cancelled_token_means_token_to_action_is_already_cancelled()
        {
            _shutdownWatcherDouble.Cancel();

            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeFalse();
            _doer.Finished.Should().BeFalse();
        }

        [Fact]
        public async Task cancellation_midflight_is_passed_to_action_via_cancellation_token()
        {
            _shutdownWatcherDouble.CancelIn(_doer.EstimatedMidflightTimeMs);
            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeFalse();
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }

    internal class Doer
    {
        public int MaxLoops => 10;
        public int LoopDelayMs => 100;
        public int EstimatedMidflightTimeMs => 500;
        public bool Finished { get; private set; } = false;
        public bool Started { get; private set; } = false;
        public bool IgnoreCancellation { get; set; } = false;


        public async Task DoAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            Started = true;
            for (int i = 0; i < MaxLoops; i++)
            {
                if (!IgnoreCancellation && cancellationToken.IsCancellationRequested) return;
                await Task.Delay(LoopDelayMs);
            }

            Finished = true;
        }
    }

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
