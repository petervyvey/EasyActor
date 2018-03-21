﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Concurrent.Fibers;
using Concurrent.Tasks;
using Concurrent.Test.TestHelper;
using FluentAssertions;
using Xunit;

namespace Concurrent.Test.Fibers
{
    public class SynchronizationContextFiberTest : IDisposable
    {
        private readonly WpfThreadingHelper _UiMessageLoop;
        private readonly SynchronizationContext _SynchronizationContext;
        private readonly SynchronizationContextFiber _SynchronizationContextFiber;

        public SynchronizationContextFiberTest()
        {
            _UiMessageLoop = new WpfThreadingHelper();
            _UiMessageLoop.Start().Wait();
            _SynchronizationContext = _UiMessageLoop.Dispatcher.Invoke(() => SynchronizationContext.Current);

            _SynchronizationContextFiber = new SynchronizationContextFiber(_SynchronizationContext);
        }

        public void Dispose()
        {
            _UiMessageLoop.Dispose();
        }

        [Fact]
        public void SynchronizationContext_Returns_Original_SynchronizationContext()
        {
            var actual = _SynchronizationContextFiber.SynchronizationContext;
            actual.Should().Be(_SynchronizationContext);
        }

        [Fact]
        public void IsAlive_Returns_True()
        {
            var actual = _SynchronizationContextFiber.IsAlive;
            actual.Should().BeTrue();
        }

        [Fact]
        public async Task Dispatch_Runs_On_UI_Thread()
        {
            var tcs = new TaskCompletionSource<Thread>();
            _SynchronizationContextFiber.Dispatch(() => tcs.TrySetResult(Thread.CurrentThread));
            var thread = await tcs.Task;
            thread.Should().Be(_UiMessageLoop.UiThread);
        }

        [Fact]
        public async Task Enqueue_Action_Runs_On_UI_Thread()
        {
            var thread = default(Thread);
            await _SynchronizationContextFiber.Enqueue(() => { thread = Thread.CurrentThread; });
            thread.Should().Be(_UiMessageLoop.UiThread);
        }

        [Fact]
        public async Task Enqueue_Func_T_Runs_On_UI_Thread()
        {
            var thread = await _SynchronizationContextFiber.Enqueue(() => Thread.CurrentThread);
            thread.Should().Be(_UiMessageLoop.UiThread);
        }

        [Fact]
        public async Task Enqueue_Func_Task_Runs_On_UI_Thread()
        {
            var thread = default(Thread);
            await _SynchronizationContextFiber.Enqueue(() =>
            {
                thread = Thread.CurrentThread;
                return TaskBuilder.Completed;
            });
            thread.Should().Be(_UiMessageLoop.UiThread);
        }

        [Fact]
        public async Task Enqueue_Func_Task_T_Runs_On_UI_Thread()
        {
            var thread = await _SynchronizationContextFiber.Enqueue(() => Task.FromResult(Thread.CurrentThread));
            thread.Should().Be(_UiMessageLoop.UiThread);
        }

        [Fact]
        public async Task Enqueue_Action_Runs_Actions_Sequencially()
        {
            var tester = new SequenceTester(_SynchronizationContextFiber);
            await tester.Stress();
            tester.Count.Should().Be(tester.MaxThreads);
        }

        [Fact]
        public async Task Enqueue_Task_Runs_Actions_Sequencially_after_await()
        {
            var tester = new SequenceTester(_SynchronizationContextFiber);
            await tester.StressTask();
            tester.Count.Should().Be(tester.MaxThreads);
        }
    }
}
