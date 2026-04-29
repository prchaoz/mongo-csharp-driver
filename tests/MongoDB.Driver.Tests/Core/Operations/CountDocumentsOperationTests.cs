/* Copyright 2018-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CountDocumentsOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.Hint.Should().BeNull();
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.IsServerDefault.Should().BeTrue();
            subject.RetryRequested.Should().BeFalse();
            subject.Skip.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new CountDocumentsOperation(null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CountDocumentsOperation(_collectionNamespace, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [CombinatorialData]
        public void Collation_get_and_set_should_work(
            [CombinatorialValues(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [CombinatorialData]
        public void Filter_get_and_set_should_work(
            [CombinatorialValues(null, "{ x : 1 }")]
            string valueString)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [CombinatorialData]
        public void Hint_get_and_set_should_work(
            [CombinatorialValues(null, "{ hint : \"x_1\" }", "{ hint : { x : 1 } }")]
            string valueString)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString)["hint"];

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [CombinatorialData]
        public void Limit_get_and_set_should_work(
            [CombinatorialValues(null, 1L, 2L)]
            long? value)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void MaxTime_get_and_set_should_work(
            [CombinatorialValues(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [CombinatorialValues(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [CombinatorialData]
        public void ReadConcern_get_and_set_should_work(
            [CombinatorialValues(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var value = level == null ? ReadConcern.Default : new ReadConcern(level.Value);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [CombinatorialData]
        public void RetryRequested_get_and_set_should_work(
            [CombinatorialValues(false, true)] bool value)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            subject.RetryRequested = value;
            var result = subject.RetryRequested;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void Skip_get_and_set_should_work(
            [CombinatorialValues(null, 1L, 2L)]
            long? value)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result(
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_no_documents_match(
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ _id : -1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(0);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [CombinatorialValues(false, true)]
            bool caseSensitive,
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary),
                Filter = BsonDocument.Parse("{ x : \"x\" }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(caseSensitive ? 1 : 2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [CombinatorialValues(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_Filter_is_set(
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ _id : 1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(1);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_Hint_is_set(
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Hint = BsonDocument.Parse("{ _id : 1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_Limit_is_set(
            [CombinatorialValues(null, 1L, 2L)]
            long? limit,
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Limit = limit
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(limit ?? 2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [CombinatorialValues(null, 1000L)]
            long? milliseconds,
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value)
            };

            // TODO: use failpoints to force a timeout?
            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_ReadConcern_is_set(
            [CombinatorialValues(null, ReadConcernLevel.Local)]
            ReadConcernLevel? level,
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level.Value);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_return_expected_result_when_Skip_is_set(
            [CombinatorialValues(null, 1L, 2L)]
            long? skip,
            [CombinatorialValues(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Skip = skip
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2 - (skip ?? 0));
        }

        [Theory]
        [CombinatorialData]
        public void Execute_should_send_session_id_when_supported(
            [CombinatorialValues(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "aggregate", async);
        }

        [Theory]
        [CombinatorialData]
        public async Task Execute_should_set_operation_name([CombinatorialValues(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);

            await VerifyOperationNameIsSet(subject, async, "aggregate");
        }

        [Fact]
        public void CreateOperation_should_return_expected_result()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedPipeline = CreateExpectedPipeline();

            var result = subject.CreateOperation();

            result.Collation.Should().BeNull();
            result.CollectionNamespace.Should().Be(_collectionNamespace);
            result.Hint.Should().BeNull();
            result.MaxTime.Should().NotHaveValue();
            result.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            result.Pipeline.Should().Equal(expectedPipeline);
            result.ReadConcern.Should().Be(ReadConcern.Default);
            result.RetryRequested.Should().BeFalse();
            result.ResultSerializer.Should().Be(BsonDocumentSerializer.Instance);
        }

        [Fact]
        public void CreateOperation_should_return_expected_result_when_Collation_is_specified()
        {
            var collation = new Collation("en_us");
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateOperation();

            result.Collation.Should().Be(collation);
        }

        [Fact]
        public void CreateOperation_should_return_expected_result_when_Hint_is_specified()
        {
            var hint = new BsonDocument("hint", 1);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Hint = hint
            };

            var result = subject.CreateOperation();

            result.Hint.Should().Be(hint);
        }

        [Fact]
        public void CreateOperation_should_return_expected_result_when_MaxTime_is_specified()
        {
            var maxTime = TimeSpan.FromSeconds(123);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateOperation();

            result.MaxTime.Should().Be(maxTime);
        }

        [Fact]
        public void CreateOperation_should_return_expected_result_when_ReadConcern_is_specified()
        {
            var readConcern = new ReadConcern(ReadConcernLevel.Snapshot);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateOperation();

            result.ReadConcern.Should().BeSameAs(readConcern);
        }

        [Theory]
        [CombinatorialData]
        public void CreateOperation_should_return_expected_result_when_RetryRequested_is_specified(
            [CombinatorialValues(false, true)] bool retryRequested)
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                RetryRequested = retryRequested
            };

            var result = subject.CreateOperation();

            result.RetryRequested.Should().Be(retryRequested);
        }

        [Fact]
        public void CreatePipeline_should_return_expected_result()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedPipeline = CreateExpectedPipeline();

            var result = subject.CreatePipeline();

            result.Should().Equal(expectedPipeline);
        }

        [Fact]
        public void CreatePipeline_should_return_expected_result_when_Filter_is_specified()
        {
            var filter = new BsonDocument("filter", 1);
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = filter
            };
            var expectedPipeline = CreateExpectedPipeline();
            expectedPipeline[0]["$match"] = filter;

            var result = subject.CreatePipeline();

            result.Should().Equal(expectedPipeline);
        }

        [Fact]
        public void CreatePipeline_should_return_expected_result_when_Skip_is_specified()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Skip = 123
            };
            var expectedPipeline = CreateExpectedPipeline();
            expectedPipeline.Insert(1, BsonDocument.Parse("{ $skip : 123 }"));

            var result = subject.CreatePipeline();

            result.Should().Equal(expectedPipeline);
        }

        [Fact]
        public void CreatePipeline_should_return_expected_result_when_Limit_is_specified()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Limit = 123
            };
            var expectedPipeline = CreateExpectedPipeline();
            expectedPipeline.Insert(1, BsonDocument.Parse("{ $limit : 123 }"));

            var result = subject.CreatePipeline();

            result.Should().Equal(expectedPipeline);
        }

        [Fact]
        public void ExtractCountFromResult_should_return_expected_result_when_list_is_empty()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var list = new List<BsonDocument>();

            var result = subject.ExtractCountFromResult(list);

            result.Should().Be(0);
        }

        [Fact]
        public void ExtractCountFromResult_should_return_expected_result_when_list_has_one_document()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var list = new List<BsonDocument> { BsonDocument.Parse("{ n : 123 }") };

            var result = subject.ExtractCountFromResult(list);

            result.Should().Be(123);
        }

        [Fact]
        public void ExtractCountFromResult_should_throw_when_list_has_more_than_one_document()
        {
            var subject = new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings);
            var list = new List<BsonDocument> { BsonDocument.Parse("{ n : 123 }"), BsonDocument.Parse("{ n : 456 }") };

            var exception = Record.Exception(() => subject.ExtractCountFromResult(list));

            exception.Should().BeOfType<MongoClientException>();
        }

        // helper methods
        private List<BsonDocument> CreateExpectedPipeline()
        {
            return new List<BsonDocument>
            {
                BsonDocument.Parse("{ $match : { } }"),
                BsonDocument.Parse("{ $group : { _id : 1, n : { $sum : 1 } } }")
            };
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(new BsonDocument { { "_id", 1 }, { "x", "x" } });
                Insert(new BsonDocument { { "_id", 2 }, { "x", "X" } });
            });
        }
    }

    internal static class CountDocumentsOperationReflector
    {
        public static AggregateOperation<BsonDocument> CreateOperation(this CountDocumentsOperation obj) => (AggregateOperation<BsonDocument>)Reflector.Invoke(obj, nameof(CreateOperation));
        public static List<BsonDocument> CreatePipeline(this CountDocumentsOperation obj) => (List<BsonDocument>)Reflector.Invoke(obj, nameof(CreatePipeline));
        public static long ExtractCountFromResult(this CountDocumentsOperation obj, List<BsonDocument> result) => (long)Reflector.Invoke(obj, nameof(ExtractCountFromResult), result);
    }
}
