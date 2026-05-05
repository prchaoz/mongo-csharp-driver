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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTestAssemblyRunner :
        XunitTestAssemblyRunnerBase<XunitTestAssemblyRunnerContext, IXunitTestAssembly, IXunitTestCollection, IXunitTestCase>
    {
        public static TimeoutEnforcingXunitTestAssemblyRunner Instance { get; } = new();

        private TimeoutEnforcingXunitTestAssemblyRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTestAssembly testAssembly,
            IReadOnlyCollection<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions,
            CancellationToken cancellationToken)
        {
            var unobservedExceptionTrackingTestCase = testCases.FirstOrDefault(IsUnobservedExceptionTrackingTestCase);
            var regularTestCases = unobservedExceptionTrackingTestCase != null
                ? testCases.Where(t => !IsUnobservedExceptionTrackingTestCase(t)).ToArray()
                : testCases;

            await using var ctxt = new XunitTestAssemblyRunnerContext(
                testAssembly, regularTestCases, executionMessageSink, executionOptions, cancellationToken);
            await ctxt.InitializeAsync();

            var summary = await Run(ctxt);

            if (unobservedExceptionTrackingTestCase != null)
            {
                await using var trailingCtxt = new XunitTestAssemblyRunnerContext(
                    testAssembly, [unobservedExceptionTrackingTestCase], executionMessageSink, executionOptions, cancellationToken);
                await trailingCtxt.InitializeAsync();
                summary.Aggregate(await Run(trailingCtxt));
            }

            return summary;
        }

        protected override ValueTask<RunSummary> RunTestCollection(
            XunitTestAssemblyRunnerContext ctxt,
            IXunitTestCollection testCollection,
            IReadOnlyCollection<IXunitTestCase> testCases) =>
            TimeoutEnforcingXunitTestCollectionRunner.Instance.Run(
                testCollection,
                testCases,
                ctxt.ExplicitOption,
                ctxt.MessageBus,
                ctxt.AssemblyTestCaseOrderer ?? DefaultTestCaseOrderer.Instance,
                ctxt.Aggregator.Clone(),
                ctxt.CancellationTokenSource,
                ctxt.AssemblyFixtureMappings);

        private static bool IsUnobservedExceptionTrackingTestCase(IXunitTestCase testCase) =>
            testCase.Traits.TryGetValue("Category", out var categories) && categories.Contains("UnobservedExceptionTracking");
    }
}
