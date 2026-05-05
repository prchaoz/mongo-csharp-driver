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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedTestsDiscoverer : IXunitTestCaseDiscoverer
    {
        private const string SpecPathPrefix = "MongoDB.Driver.Tests.Specifications";

        public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute)
        {
            var theoryAttribute = (UnifiedTestsTheoryAttribute)factAttribute;
            var testClass = testMethod.TestClass.Class;
            var testsToSkip = GetHashSetMember(testClass, theoryAttribute.SkippedTestsProvider);
            var filesToSkip = GetHashSetMember(testClass, theoryAttribute.SkippedFilesProvider);

            var testsFactory = new UnifiedTestCaseFactory(theoryAttribute.Path, testsToSkip, filesToSkip);

            var testCases = new List<IXunitTestCase>();
            foreach (var testCaseArguments in testsFactory)
            {
                var jsonTestCase = (JsonDrivenTestCase)testCaseArguments[0];

                var details = TestIntrospectionHelper.GetTestCaseDetails(
                    discoveryOptions,
                    testMethod,
                    factAttribute,
                    testMethodArguments: [jsonTestCase],
                    label: null);

                var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in testMethod.Traits)
                {
                    traits[kvp.Key] = new HashSet<string>(kvp.Value);
                }

                testCases.Add(new XunitTestCase(
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
                    testMethodArguments: [jsonTestCase],
                    sourceFilePath: jsonTestCase.Shared["_localPath"].AsString,
                    sourceLineNumber: jsonTestCase.Test["_lineNumber"].AsInt32,
                    timeout: details.Timeout));
            }

            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
        }

        private static HashSet<string> GetHashSetMember(Type testClass, string memberName)
        {
            if (memberName == null || testClass == null)
            {
                return null;
            }

            var provider = testClass.GetField(memberName, BindingFlags.NonPublic | BindingFlags.Static);
            return provider?.GetValue(null) as HashSet<string>;
        }

        private sealed class UnifiedTestCaseFactory(string path, HashSet<string> testsToSkip, HashSet<string> filesToSkip) : JsonDrivenTestCaseFactory
        {
            private readonly HashSet<string> _filesToSkip = filesToSkip;
            private readonly HashSet<string> _testsToSkip = testsToSkip;
            private readonly string _path = $"{SpecPathPrefix}.{path}.";

            protected override string PathPrefix => _path;

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var path = document["_path"].AsString;
                var fileName = path.Replace(PathPrefix, "");

                using var stream = Assembly.GetManifestResourceStream(path);
                using var streamReader = new StreamReader(stream);
                var lines = streamReader.ReadToEnd().Split('\n')
                    .Select((Line, Index) => (Line, Index))
                    .Where(p => p.Line.Contains("description"))
                    .ToArray();

                var relativeLocalPath = path
                    .Replace(SpecPathPrefix, "")
                    .Replace(".json", "")
                    .Replace("_", "-")
                    .Replace('.', '\\');

                document
                    .Add("_localPath", Path.GetFullPath($"..\\..\\..\\..\\..\\specifications{relativeLocalPath}.json"))
                    .Add("_fileName", fileName);

                foreach (var testCase in base.CreateTestCases(document))
                {
                    if (_testsToSkip?.Contains(testCase.Name) == true)
                    {
                        continue;
                    }

                    var description = testCase.Test["description"].AsString;
                    var lineNumber = lines.FirstOrDefault(p => p.Line.Contains(description)).Index;
                    testCase.Test.Add("_lineNumber", lineNumber);

                    var test = testCase.Test.Add("async", false);
                    var name = $"{fileName}:{testCase.Name}:async={false}";
                    yield return new JsonDrivenTestCase(name, testCase.Shared, test);

                    test = testCase.Test.DeepClone().AsBsonDocument.Set("async", true);
                    name = $"{fileName}:{testCase.Name}:async={true}";
                    yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                }
            }

            protected override string GetTestCaseName(BsonDocument shared, BsonDocument test, int index) =>
                GetTestName(test, index);

            protected override bool ShouldReadJsonDocument(string path) =>
                base.ShouldReadJsonDocument(path) &&
                _filesToSkip?.Any(path.Contains) != true;
        }
    }

    [XunitTestCaseDiscoverer(typeof(UnifiedTestsDiscoverer))]
    public class UnifiedTestsTheoryAttribute(
        string path,
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : FactAttribute(sourceFilePath, sourceLineNumber)
    {
        public string Path { get; set; } = path;
        public string SkippedTestsProvider { get; set; } = "__ignoredTests";
        public string SkippedFilesProvider { get; set; } = "__ignoredTestFiles";
    }
}
