using AzureWebjobHost;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AzureWebjobHostTests
{
    public class JobHostTest : IDisposable
    {
        private readonly JobHost _host;
        private readonly WebJobShutdownWatcherDouble _shutdownWatcherDouble;
        private readonly ShutdownHandleDouble _shutdownHandleDouble;
        private readonly Doer _doer;

        public JobHostTest()
        {
            _shutdownWatcherDouble = new WebJobShutdownWatcherDouble();
            _shutdownHandleDouble = new ShutdownHandleDouble();
            _host = new JobHost(_shutdownWatcherDouble, new ShutdownHandleDoubleFactory(_shutdownHandleDouble), default);
            _doer = new Doer();
        }

        [Fact]
        public async Task RunAsync_runs_uncancelled_tasks_to_completion()
        {
            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeTrue();
        }

        [Fact]
        public async Task RunAsyncT_runs_uncancelled_tasks_to_completion_and_returns_result()
        {
            var result = await _host.RunAsync(_doer.AlwaysReturnValueAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeTrue();
            result.Should().Be(DoerReturnValue.Finished);
        }

        [Fact]
        public async Task RunAsync_passes_cancellation_onto_action()
        {
            _shutdownWatcherDouble.CancelIn(_doer.EstimatedMidflightTimeMs);
            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsyncT_passes_cancellation_onto_action()
        {
            _shutdownWatcherDouble.CancelIn(_doer.EstimatedMidflightTimeMs);
            var result = await _host.RunAsync(_doer.AlwaysReturnValueAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeFalse();
            result.Should().Be(DoerReturnValue.Started);
        }

        [Fact]
        public async Task RunAsync_passes_already_cancelled_token_to_method_if_token_cancelled_beforehand()
        {
            _shutdownWatcherDouble.Cancel();

            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeFalse();
            _doer.Finished.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsyncT_passes_already_cancelled_token_to_method_if_token_cancelled_beforehand()
        {
            _shutdownWatcherDouble.Cancel();

            var result = await _host.RunAsync(_doer.AlwaysReturnValueAsync);

            _doer.Started.Should().BeFalse();
            _doer.Finished.Should().BeFalse();
            result.Should().Be(DoerReturnValue.Unstarted);
        }

        [Fact]
        public async Task RunAsync_propagates_up_exceptions_thrown_by_action()
        {
            _shutdownWatcherDouble.Cancel();

            Func<Task> action = async () => await _host.RunAsync(_doer.ThrowWhenCancelled);

            await action.Should().ThrowAsync<TimeZoneNotFoundException>();
        }

        [Fact]
        public async Task RunAsyncT_propagates_up_exceptions_thrown_by_action()
        {
            _shutdownWatcherDouble.Cancel();

            Func<Task> action = async () => await _host.RunAsync(_doer.ThrowWhenCancelledOrReturnValue);

            await action.Should().ThrowAsync<TimeZoneNotFoundException>();
        }

        [Fact]
        public async Task RunAsync_listens_to_ShutdownHandle_ProcessExit_as_well()
        {
            _ = _shutdownHandleDouble.ProcessExitIn(_doer.EstimatedMidflightTimeMs);

            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsync_listens_to_ShutdownHandle_CancelKey_as_well()
        {
            _shutdownHandleDouble.CancelKeyPressIn(_doer.EstimatedMidflightTimeMs);

            await _host.RunAsync(_doer.DoAsync);

            _doer.Started.Should().BeTrue();
            _doer.Finished.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsync_in_case_of_shutdownHandle_event_tries_to_block_shutdown_while_action_deals_with_cancellation_token()
        {
            _doer.PauseOnCancellation = 2000;

            var shutdown = _shutdownHandleDouble.ProcessExitIn(_doer.EstimatedMidflightTimeMs);
            _ = _host.RunAsync(_doer.DoAsync);

            await Task.Delay(1000);

            // Ie it's waiting for us
            shutdown.IsCompleted.Should().BeFalse();

            shutdown.Wait(2000);
            shutdown.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task RunAsyncT_in_case_of_shutdownHandle_event_tries_to_block_shutdown_while_action_deals_with_cancellation_token()
        {
            _doer.PauseOnCancellation = 2000;

            var shutdown = _shutdownHandleDouble.ProcessExitIn(_doer.EstimatedMidflightTimeMs);
            _ = _host.RunAsync(_doer.AlwaysReturnValueAsync);

            await Task.Delay(1000);

            // Ie it's waiting for us
            shutdown.IsCompleted.Should().BeFalse();

            shutdown.Wait(2000);
            shutdown.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task RunAsync_in_case_of_shutdownHandle_event_stops_blocking_shutdown_after_4_seconds()
        {
            _doer.PauseOnCancellation = 6000;

            var shutdown = _shutdownHandleDouble.ProcessExitIn(_doer.EstimatedMidflightTimeMs);
            var run = _host.RunAsync(_doer.DoAsync);

            shutdown.Wait(5000);
            shutdown.IsCompleted.Should().BeTrue();
            // Ie shutdown gave up waiting
            run.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsyncT_in_case_of_shutdownHandle_event_stops_blocking_shutdown_after_4_seconds()
        {
            _doer.PauseOnCancellation = 6000;

            var shutdown = _shutdownHandleDouble.ProcessExitIn(_doer.EstimatedMidflightTimeMs);
            var run = _host.RunAsync(_doer.AlwaysReturnValueAsync);

            shutdown.Wait(5000);
            shutdown.IsCompleted.Should().BeTrue();
            // Ie shutdown gave up waiting
            run.IsCompleted.Should().BeFalse();
        }

        public void Dispose()
        {
            _host?.Dispose();
        }

        private class Doer
        {
            public int MaxLoops => 10;
            public int LoopDelayMs => 100;
            public int EstimatedMidflightTimeMs => 500;
            public bool Finished { get; private set; } = false;
            public bool Started { get; private set; } = false;
            public bool IgnoreCancellation { get; set; } = false;
            public int? PauseOnCancellation { get; set; } = default;

            public async Task DoAsync(CancellationToken cancellationToken)
            {
                await AlwaysReturnValueAsync(cancellationToken);
            }

            public async Task<DoerReturnValue> AlwaysReturnValueAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested) return DoerReturnValue.Unstarted;

                Started = true;
                for (int i = 0; i < MaxLoops; i++)
                {
                    if (!IgnoreCancellation && cancellationToken.IsCancellationRequested)
                    {
                        if (PauseOnCancellation.HasValue) await Task.Delay(PauseOnCancellation.Value);
                        return DoerReturnValue.Started;
                    }
                    await Task.Delay(LoopDelayMs);
                }

                Finished = true;
                return DoerReturnValue.Finished;
            }

            public async Task ThrowWhenCancelled(CancellationToken cancellationToken)
            {
                await ThrowWhenCancelledOrReturnValue(cancellationToken);
            }

            public async Task<DoerReturnValue> ThrowWhenCancelledOrReturnValue(CancellationToken cancellationToken)
            {
                var result = await AlwaysReturnValueAsync(cancellationToken);

                if (result == DoerReturnValue.Unstarted || result == DoerReturnValue.Started)
                    throw new TimeZoneNotFoundException();

                return result;
            }
        }

        private enum DoerReturnValue {
            Unstarted, Started, Finished
        }
    }
}
