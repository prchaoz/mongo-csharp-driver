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
    internal sealed class TimeoutEnforcingXunitTestClassRunner :
        XunitTestClassRunnerBase<XunitTestClassRunnerContext, IXunitTestClass, IXunitTestMethod, IXunitTestCase>
    {
        public static TimeoutEnforcingXunitTestClassRunner Instance { get; } = new();

        private TimeoutEnforcingXunitTestClassRunner() { }

        public async ValueTask<RunSummary> Run(
            IXunitTestClass testClass,
            IReadOnlyCollection<IXunitTestCase> testCases,
            ExplicitOption explicitOption,
            IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            FixtureMappingManager collectionFixtureMappings)
        {
            await using var ctxt = new XunitTestClassRunnerContext(
                testClass, testCases, explicitOption, messageBus, testCaseOrderer,
                aggregator, cancellationTokenSource, collectionFixtureMappings);
            await ctxt.InitializeAsync();
            return await ctxt.Aggregator.RunAsync(() => Run(ctxt), default);
        }

        protected override ValueTask<RunSummary> RunTestMethod(
            XunitTestClassRunnerContext ctxt,
            IXunitTestMethod testMethod,
            IReadOnlyCollection<IXunitTestCase> testCases,
            object[] constructorArguments) =>
            TimeoutEnforcingXunitTestMethodRunner.Instance.Run(
                testMethod,
                testCases,
                ctxt.ExplicitOption,
                ctxt.MessageBus,
                ctxt.Aggregator.Clone(),
                ctxt.CancellationTokenSource,
                constructorArguments);
    }
}
