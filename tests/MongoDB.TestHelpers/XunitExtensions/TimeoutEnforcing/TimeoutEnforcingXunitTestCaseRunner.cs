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
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTestCaseRunner :
        XunitTestCaseRunnerBase<XunitTestCaseRunnerContext, IXunitTestCase, IXunitTest>
    {
        public static TimeoutEnforcingXunitTestCaseRunner Instance { get; } = new();

        private TimeoutEnforcingXunitTestCaseRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTestCase testCase,
            IReadOnlyCollection<IXunitTest> tests,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            string displayName,
            string skipReason,
            ExplicitOption explicitOption,
            object[] constructorArguments)
        {
            await using var ctxt = new XunitTestCaseRunnerContext(
                testCase, tests, messageBus, aggregator, cancellationTokenSource,
                displayName, skipReason, explicitOption, constructorArguments);
            await ctxt.InitializeAsync();
            return await Run(ctxt);
        }

        protected override ValueTask<RunSummary> RunTest(
            XunitTestCaseRunnerContext ctxt,
            IXunitTest test) =>
            TimeoutEnforcingTestRunner.Instance.Run(
                test,
                ctxt.MessageBus,
                ctxt.ConstructorArguments,
                ctxt.ExplicitOption,
                ctxt.Aggregator.Clone(),
                ctxt.CancellationTokenSource,
                ctxt.BeforeAfterTestAttributes);
    }
}
