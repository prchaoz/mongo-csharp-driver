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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing;

[XunitTestCaseDiscoverer(typeof(UnobservedExceptionTestDiscoverer))]
public class UnobservedExceptionTrackingFactAttribute : FactAttribute
{
}

public class UnobservedExceptionTestDiscoverer : IXunitTestCaseDiscoverer
{
    private static readonly ConcurrentBag<string> __unobservedExceptions = new();

    public UnobservedExceptionTestDiscoverer()
    {
        TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionEventHandler;
    }

    public static IReadOnlyCollection<string> UnobservedExceptions => __unobservedExceptions;

    public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute, label: null);

        var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in testMethod.Traits)
        {
            traits[kvp.Key] = new HashSet<string>(kvp.Value);
        }
        if (!traits.TryGetValue("Category", out var categories))
        {
            categories = new HashSet<string>();
            traits["Category"] = categories;
        }
        categories.Add("UnobservedExceptionTracking");

        var testCase = new XunitTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.Explicit,
            details.SkipExceptions,
            details.SkipReason,
            details.SkipType,
            details.SkipUnless,
            details.SkipWhen,
            traits,
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            timeout: details.Timeout);

        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>([testCase]);
    }

    private static void UnobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs unobservedException) =>
        __unobservedExceptions.Add(unobservedException.Exception.ToString());
}
