/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingTestRunner : XunitTestRunnerBase<XunitTestRunnerContext, IXunitTest>
    {
        public static TimeoutEnforcingTestRunner Instance { get; } = new();

        private TimeoutEnforcingTestRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTest test,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExplicitOption explicitOption,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            IReadOnlyCollection<IBeforeAfterTestAttribute> beforeAfterAttributes)
        {
            await using var ctxt = new XunitTestRunnerContext(
                test, messageBus, explicitOption, aggregator, cancellationTokenSource,
                beforeAfterAttributes, constructorArguments);
            await ctxt.InitializeAsync();
            return await Run(ctxt);
        }

        protected override async ValueTask<TimeSpan> RunTest(XunitTestRunnerContext ctxt)
        {
            if (Debugger.IsAttached)
            {
                return await base.RunTest(ctxt);
            }

            var timeout = ctxt.Test.Timeout > 0
                ? TimeSpan.FromMilliseconds(ctxt.Test.Timeout)
                : XunitExtensionsConstants.DefaultTestTimeout;

            var stopwatch = Stopwatch.StartNew();
            await ctxt.Aggregator.RunAsync(async () =>
            {
                Task baseTask = Task.Run(async () =>
                {
                    await new YieldNoContextAwaitable();
                    await base.RunTest(ctxt);
                });
                var resultTask = await Task.WhenAny(baseTask, Task.Delay(timeout));
                if (resultTask != baseTask)
                {
                    throw TestTimeoutException.ForTimedOutTest((int)timeout.TotalMilliseconds);
                }
            });
            return stopwatch.Elapsed;
        }

        protected override async ValueTask<TimeSpan> InvokeTest(XunitTestRunnerContext ctxt, object testClassInstance)
        {
            var testExceptionHandler = testClassInstance as ITestExceptionHandler;
            using var unobservedExceptionDebugger = UnobservedExceptionDebugger.Create();
            try
            {
                var elapsed = await base.InvokeTest(ctxt, testClassInstance);

                if (ctxt.Aggregator.HasExceptions && testExceptionHandler != null)
                {
                    var exception = ctxt.Aggregator.ToException();
                    if (exception is not SkipException)
                    {
                        testExceptionHandler.HandleException(exception);
                    }
                }

                return elapsed;
            }
            catch (Exception exception)
            {
                testExceptionHandler?.HandleException(exception);
                throw;
            }
        }

        // Copy of MongoDB.Driver.Core.Misc.TaskExtensions.YieldNoContextAwaitable.
        private struct YieldNoContextAwaitable
        {
            public YieldNoContextAwaiter GetAwaiter() => new();

            public struct YieldNoContextAwaiter : ICriticalNotifyCompletion
            {
                public bool IsCompleted => false;

                public void OnCompleted(Action continuation) =>
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

                public void UnsafeOnCompleted(Action continuation) =>
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

                public void GetResult() { }
            }
        }

        private class UnobservedExceptionDebugger : IDisposable
        {
            private Exception _unobservedException;

            private UnobservedExceptionDebugger()
            {
                TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionEventHandler;
            }

            public static UnobservedExceptionDebugger Create()
            {
#if UNOBSERVED_TASK_EXCEPTION_DEBUGGING
                return new UnobservedExceptionDebugger();
#else
                return null;
#endif
            }

            public void Dispose()
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                TaskScheduler.UnobservedTaskException -= UnobservedTaskExceptionEventHandler;

                if (_unobservedException != null)
                {
                    throw _unobservedException;
                }
            }

            private void UnobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs unobservedExceptionArgs)
            {
                _unobservedException = unobservedExceptionArgs.Exception;
            }
        }
    }
}
