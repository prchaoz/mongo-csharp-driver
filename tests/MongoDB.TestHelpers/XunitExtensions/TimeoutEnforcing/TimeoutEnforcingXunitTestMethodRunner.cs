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

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTestMethodRunner :
        XunitTestMethodRunnerBase<XunitTestMethodRunnerContext, IXunitTestMethod, IXunitTestCase>
    {
        public static TimeoutEnforcingXunitTestMethodRunner Instance { get; } = new();

        private TimeoutEnforcingXunitTestMethodRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTestMethod testMethod,
            IReadOnlyCollection<IXunitTestCase> testCases,
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            object[] constructorArguments)
        {
            await using var ctxt = new XunitTestMethodRunnerContext(
                testMethod, testCases, explicitOption, messageBus, aggregator,
                cancellationTokenSource, constructorArguments);
            await ctxt.InitializeAsync();
            return await Run(ctxt);
        }

        protected override async ValueTask<RunSummary> RunTestCase(
            XunitTestMethodRunnerContext ctxt,
            IXunitTestCase testCase)
        {
            if (testCase is ISelfExecutingXunitTestCase selfExecutingTestCase)
            {
                return await selfExecutingTestCase.Run(
                    ctxt.ExplicitOption,
                    ctxt.MessageBus,
                    ctxt.ConstructorArguments,
                    ctxt.Aggregator.Clone(),
                    ctxt.CancellationTokenSource);
            }

            var aggregator = ctxt.Aggregator.Clone();
            var tests = await aggregator.RunAsync(testCase.CreateTests, []);

            if (aggregator.ToException() is System.Exception ex)
            {
                if (ex.Message?.StartsWith(DynamicSkipToken.Value, System.StringComparison.Ordinal) == true)
                {
                    return XunitRunnerHelper.SkipTestCases(
                        ctxt.MessageBus,
                        ctxt.CancellationTokenSource,
                        [testCase],
                        ex.Message.Substring(DynamicSkipToken.Value.Length),
                        sendTestCaseMessages: false);
                }
                if (testCase.SkipExceptions?.Contains(ex.GetType()) == true)
                {
                    return XunitRunnerHelper.SkipTestCases(
                        ctxt.MessageBus,
                        ctxt.CancellationTokenSource,
                        [testCase],
                        !string.IsNullOrEmpty(ex.Message)
                            ? ex.Message
                            : string.Format(CultureInfo.CurrentCulture, "Exception of type '{0}' was thrown", ex.GetType().FullName),
                        sendTestCaseMessages: false);
                }

                return XunitRunnerHelper.FailTestCases(
                    ctxt.MessageBus,
                    ctxt.CancellationTokenSource,
                    [testCase],
                    ex,
                    sendTestCaseMessages: false);
            }

            return await TimeoutEnforcingXunitTestCaseRunner.Instance.Run(
                testCase,
                tests,
                ctxt.MessageBus,
                aggregator,
                ctxt.CancellationTokenSource,
                testCase.TestCaseDisplayName,
                testCase.SkipReason,
                ctxt.ExplicitOption,
                ctxt.ConstructorArguments);
        }
    }
}
