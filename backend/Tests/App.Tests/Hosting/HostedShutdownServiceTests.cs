using System.IO.Pipes;
using Business.Executions;
using Core.Enums;
using Core.Models.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace App.Tests.Hosting
{
    /// <summary>
    /// How the backend stops: when Electron closes its stdin, and in every stop with the execution
    /// still going stopped first. A real pipe, since a read on one cannot be cancelled, and that is
    /// what a stop must not wait for.
    /// </summary>
    public sealed class HostedShutdownServiceTests : IDisposable
    {
        private static readonly TimeSpan LONG_ENOUGH = TimeSpan.FromSeconds(5);

        private readonly AnonymousPipeServerStream _electron = new AnonymousPipeServerStream(PipeDirection.Out);
        private readonly AnonymousPipeClientStream _stdin;
        private readonly FakeLifetime _lifetime = new FakeLifetime();
        private readonly FakeExecutionEngine _engine = new FakeExecutionEngine();
        private readonly HostedShutdownService _shutdown;

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public HostedShutdownServiceTests()
        {
            _stdin = new AnonymousPipeClientStream(PipeDirection.In, _electron.ClientSafePipeHandle);
            _shutdown = new HostedShutdownService(_lifetime, _engine, _stdin);
        }

        public void Dispose()
        {
            _electron.Dispose();
            _shutdown.Dispose();
            _stdin.Dispose();
        }

        [Fact]
        public async Task Closing_stdin_stops_the_app()
        {
            await StartReadingAsync();
            _lifetime.IsStopAsked.ShouldBeFalse();

            _electron.Dispose();
            await _shutdown.ExecuteTask!.WaitAsync(LONG_ENOUGH, Ct);

            _lifetime.IsStopAsked.ShouldBeTrue();
        }

        [Fact]
        public async Task A_stop_asked_for_elsewhere_does_not_wait_for_stdin()
        {
            await StartReadingAsync();

            await _shutdown.StopAsync(Ct).WaitAsync(LONG_ENOUGH, Ct);

            _lifetime.IsStopAsked.ShouldBeFalse();
        }

        [Fact]
        public async Task Stopping_stops_the_execution_still_going_and_waits_for_its_history()
        {
            _engine.IsRunning = true;

            Task stopping = _shutdown.StoppingAsync(Ct);

            _engine.StopAsked.IsCompleted.ShouldBeTrue();
            stopping.IsCompleted.ShouldBeFalse();

            _engine.Finish();
            await stopping.WaitAsync(LONG_ENOUGH, Ct);
        }

        [Fact]
        public async Task Stopping_with_no_execution_going_stops_nothing()
        {
            await _shutdown.StoppingAsync(Ct).WaitAsync(LONG_ENOUGH, Ct);

            _engine.StopAsked.IsCompleted.ShouldBeFalse();
        }

        // The pipes are registered after it, so they would stop first if the order were all there was.
        [Fact]
        public async Task In_the_host_nothing_stops_before_the_execution_has_written_its_history()
        {
            FakePipes pipes = new FakePipes();
            _engine.IsRunning = true;

            HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
            builder.Services.AddSingleton<IExecutionEngine>(_engine);
            builder.Services.AddHostedService(x => ActivatorUtilities.CreateInstance<HostedShutdownService>(x, _stdin));
            builder.Services.AddHostedService(_ => pipes);
            using IHost host = builder.Build();
            await host.StartAsync(Ct);

            _electron.Dispose();
            Task shutdown = host.WaitForShutdownAsync(Ct);
            await _engine.StopAsked.WaitAsync(LONG_ENOUGH, Ct);

            pipes.IsStopped.ShouldBeFalse();

            _engine.Finish();
            await shutdown.WaitAsync(LONG_ENOUGH, Ct);

            pipes.IsStopped.ShouldBeTrue();
        }

        // A byte taken from the pipe is the shutdown service reading it.
        private async Task StartReadingAsync()
        {
            await _shutdown.StartAsync(Ct);

            _electron.WriteByte(1);
            await Task.Run(_electron.WaitForPipeDrain, Ct).WaitAsync(LONG_ENOUGH, Ct);
        }

        private sealed class FakeLifetime : IHostApplicationLifetime
        {
            public bool IsStopAsked { get; private set; }

            public CancellationToken ApplicationStarted
            {
                get { return CancellationToken.None; }
            }

            public CancellationToken ApplicationStopping
            {
                get { return CancellationToken.None; }
            }

            public CancellationToken ApplicationStopped
            {
                get { return CancellationToken.None; }
            }

            public void StopApplication()
            {
                IsStopAsked = true;
            }
        }

        // An execution goes on until Finish: its walk over and its history written.
        private sealed class FakeExecutionEngine : IExecutionEngine
        {
            private readonly TaskCompletionSource _stopAsked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool IsRunning { get; set; }
            public RunStateEnum State { get; } = RunStateEnum.FINISHED;
            public int ExecutionId { get; }
            public int FlowId { get; }
            public int StepCount { get; }

            public Task StopAsked
            {
                get { return _stopAsked.Task; }
            }

            public Task Completion
            {
                get
                {
                    if (IsRunning)
                        return _finished.Task;

                    return Task.CompletedTask;
                }
            }

            public void Finish()
            {
                IsRunning = false;
                _finished.TrySetResult();
            }

            public void Stop()
            {
                _stopAsked.TrySetResult();
            }

            public Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
            {
                throw new NotSupportedException();
            }

            public void Pause()
            {
            }

            public void Continue()
            {
            }

            public void StepInto()
            {
            }

            public void StepOver()
            {
            }

            public void SetBreakpoints(IEnumerable<int> flowStepIds)
            {
            }

            public bool IsExecuting(int flowId)
            {
                return false;
            }
        }

        private sealed class FakePipes : IHostedService
        {
            public bool IsStopped { get; private set; }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                IsStopped = true;
                return Task.CompletedTask;
            }
        }
    }
}
