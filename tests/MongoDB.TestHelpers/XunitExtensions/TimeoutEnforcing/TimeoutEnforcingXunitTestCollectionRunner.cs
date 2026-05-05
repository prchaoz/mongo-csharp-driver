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
    internal sealed class TimeoutEnforcingXunitTestCollectionRunner :
        XunitTestCollectionRunnerBase<XunitTestCollectionRunnerContext, IXunitTestCollection, IXunitTestClass, IXunitTestCase>
    {
        public static TimeoutEnforcingXunitTestCollectionRunner Instance { get; } = new();

        private TimeoutEnforcingXunitTestCollectionRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTestCollection testCollection,
            IReadOnlyCollection<IXunitTestCase> testCases,
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            FixtureMappingManager assemblyFixtureMappings)
        {
            await using var ctxt = new XunitTestCollectionRunnerContext(
                testCollection, testCases, explicitOption, messageBus, testCaseOrderer,
                aggregator, cancellationTokenSource, assemblyFixtureMappings);
            await ctxt.InitializeAsync();
            return await Run(ctxt);
        }

        protected override ValueTask<RunSummary> RunTestClass(
            XunitTestCollectionRunnerContext ctxt,
            IXunitTestClass testClass,
            IReadOnlyCollection<IXunitTestCase> testCases) =>
            TimeoutEnforcingXunitTestClassRunner.Instance.Run(
                testClass,
                testCases,
                ctxt.ExplicitOption,
                ctxt.MessageBus,
                ctxt.TestCaseOrderer,
                ctxt.Aggregator.Clone(),
                ctxt.CancellationTokenSource,
                ctxt.CollectionFixtureMappings);
    }
}
