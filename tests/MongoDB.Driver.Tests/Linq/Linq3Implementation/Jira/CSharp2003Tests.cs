﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2003Tests : LinqIntegrationTest<CSharp2003Tests.ClassFixture>
    {
        public CSharp2003Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Find_BitsAllClear_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var find = collection.Find(x => (x.E & mask) == 0);

            var filter = Translate(collection, find.Filter);
            filter.Should().Be("{ E : { $bitsAllClear : 6 } }");

            var results = find.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 8);
        }

        [Fact]
        public void Find_BitsAllSet_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var find = collection.Find(x => (x.E & mask) == mask);

            var filter = Translate(collection, find.Filter);
            filter.Should().Be("{ E : { $bitsAllSet : 6 } }");

            var results = find.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(6);
        }

        [Fact]
        public void Find_BitsAnyClear_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var find = collection.Find(x => (x.E & mask) != mask);

            var filter = Translate(collection, find.Filter);
            filter.Should().Be("{ E : { $bitsAnyClear : 6 } }");

            var results = find.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 4, 8);
        }

        [Fact]
        public void Find_BitsAnySet_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var find = collection.Find(x => (x.E & mask) != 0);

            var filter = Translate(collection, find.Filter);
            filter.Should().Be("{ E : { $bitsAnySet : 6 } }");

            var results = find.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(2, 4, 6);
        }

        [Fact]
        public void Where_BitsAllClear_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var queryable = collection.AsQueryable().Where(x => (x.E & mask) == 0);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { E : { $bitsAllClear : 6 } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 8);
        }

        [Fact]
        public void Where_BitsAllSet_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var queryable = collection.AsQueryable().Where(x => (x.E & mask) == mask);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { E : { $bitsAllSet : 6 } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(6);
        }

        [Fact]
        public void Where_BitsAnyClear_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var queryable = collection.AsQueryable().Where(x => (x.E & mask) != mask);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { E : { $bitsAnyClear : 6 } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 4, 8);
        }

        [Fact]
        public void Where_BitsAnySet_should_work()
        {
            var collection = Fixture.Collection;
            var mask = E.E2 | E.E4;
            var queryable = collection.AsQueryable().Where(x => (x.E & mask) != 0);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { E : { $bitsAnySet : 6 } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(2, 4, 6);
        }

        [Flags]
#pragma warning disable CA1714
        public enum E
#pragma warning restore CA1714
        {
            E1 = 1,
            E2 = 2,
            E4 = 4,
            E8 = 8
        }

        public class C
        {
            public int Id { get; set; }
            public E E;
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, E = E.E1 },
                new C { Id = 2, E = E.E2 },
                new C { Id = 4, E = E.E4 },
                new C { Id = 6, E = E.E2 | E.E4 },
                new C { Id = 8, E = E.E8 }
            ];
        }
    }
}
