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
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class CoreSessionOptionsTests
    {
        [Theory]
        [CombinatorialData]
        public void constructor_should_initialize_instance(
            [CombinatorialValues(false, true)] bool nullDefaultTransactionOptions,
            [CombinatorialValues(false, true)] bool isCausallyConsistent,
            [CombinatorialValues(false, true)] bool isImplicit)
        {
            var defaultTransactionOptions = nullDefaultTransactionOptions ? null : new TransactionOptions();

            var result = new CoreSessionOptions(
                defaultTransactionOptions: defaultTransactionOptions,
                isCausallyConsistent: isCausallyConsistent,
                isImplicit: isImplicit);

            result.DefaultTransactionOptions.Should().BeSameAs(defaultTransactionOptions);
            result.IsCausallyConsistent.Should().Be(isCausallyConsistent);
            result.IsImplicit.Should().Be(isImplicit);
        }

        [Fact]
        public void constructor_should_initialize_instance_with_default_values()
        {
            var result = new CoreSessionOptions();

            result.DefaultTransactionOptions.Should().BeNull();
            result.IsCausallyConsistent.Should().BeFalse();
            result.IsImplicit.Should().BeFalse();
            result.SnapshotTime.Should().BeNull();
        }

        [Fact]
        public void constructor_should_initialize_SnapshotTime_when_isSnapshot_is_true()
        {
            var snapshotTime = new BsonTimestamp(1234567890, 1);

            var result = new CoreSessionOptions(isSnapshot: true, snapshotTime: snapshotTime);

            result.SnapshotTime.Should().Be(snapshotTime);
            result.IsSnapshot.Should().BeTrue();
        }

        [Theory]
        [CombinatorialData]
        public void DefaultTransactionOptions_should_return_expected_result(
            [CombinatorialValues(false, true)] bool nullDefaultTransactionOptions)
        {
            var defaultTransactionOptions = nullDefaultTransactionOptions ? null : new TransactionOptions();
            var subject = new CoreSessionOptions(defaultTransactionOptions: defaultTransactionOptions);

            var result = subject.DefaultTransactionOptions;

            result.Should().BeSameAs(defaultTransactionOptions);
        }

        [Theory]
        [CombinatorialData]
        public void IsCausallyConsistent_should_return_expected_result(
            [CombinatorialValues(false, true)] bool value)
        {
            var subject = new CoreSessionOptions(isCausallyConsistent: value);

            var result = subject.IsCausallyConsistent;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void IsImplicit_should_return_expected_result(
            [CombinatorialValues(false, true)] bool value)
        {
            var subject = new CoreSessionOptions(isImplicit: value);

            var result = subject.IsImplicit;

            result.Should().Be(value);
        }

        [Theory]
        [CombinatorialData]
        public void SnapshotTime_should_return_expected_result(
            [CombinatorialValues(false, true)] bool nullValue)
        {
            var snapshotTime = nullValue ? null : new BsonTimestamp(1234567890, 1);
            var subject = new CoreSessionOptions(isSnapshot: true, snapshotTime: snapshotTime);

            var result = subject.SnapshotTime;

            result.Should().Be(snapshotTime);
        }
    }
}
