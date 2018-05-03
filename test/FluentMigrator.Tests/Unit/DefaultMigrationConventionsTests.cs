#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Linq;
using System.Reflection;

using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Model;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Infrastructure;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SqlServer;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Shouldly;

namespace FluentMigrator.Tests.Unit
{
    [TestFixture]
    public class DefaultMigrationConventionsTests
    {
        private static readonly IMigrationRunnerConventions _default = DefaultMigrationRunnerConventions.Instance;

        [Test]
        public void GetPrimaryKeyNamePrefixesTableNameWithPKAndUnderscore()
        {
            var expr = new CreateColumnExpression()
            {
                Column =
                {
                    TableName = "Foo",
                    IsPrimaryKey = true,
                }
            };

            var processed = expr.Apply(ConventionSets.NoSchemaName);
            processed.Column.PrimaryKeyName.ShouldBe("PK_Foo");
        }

        [Test]
        public void GetForeignKeyNameReturnsValidForeignKeyNameForSimpleForeignKey()
        {
            var expr = new CreateForeignKeyExpression()
            {
                ForeignKey =
                {
                    ForeignTable = "Users",
                    ForeignColumns = new[] { "GroupId" },
                    PrimaryTable = "Groups",
                    PrimaryColumns = new[] { "Id" }
                }
            };

            var processed = expr.Apply(ConventionSets.NoSchemaName);

            processed.ForeignKey.Name.ShouldBe("FK_Users_GroupId_Groups_Id");
        }

        [Test]
        public void GetForeignKeyNameReturnsValidForeignKeyNameForComplexForeignKey()
        {
            var expr = new CreateForeignKeyExpression()
            {
                ForeignKey =
                {
                    ForeignTable = "Users",
                    ForeignColumns = new[] { "ColumnA", "ColumnB" },
                    PrimaryTable = "Groups",
                    PrimaryColumns = new[] { "ColumnC", "ColumnD" }
                }
            };

            var processed = expr.Apply(ConventionSets.NoSchemaName);

            processed.ForeignKey.Name.ShouldBe("FK_Users_ColumnA_ColumnB_Groups_ColumnC_ColumnD");
        }

        [Test]
        public void GetIndexNameReturnsValidIndexNameForSimpleIndex()
        {
            var expr = new CreateIndexExpression()
            {
                Index =
                {
                    TableName = "Bacon",
                    Columns =
                    {
                        new IndexColumnDefinition { Name = "BaconName", Direction = Direction.Ascending }
                    }
                }
            };

            var processed = expr.Apply(ConventionSets.NoSchemaName);

            processed.Index.Name.ShouldBe("IX_Bacon_BaconName");
        }

        [Test]
        public void GetIndexNameReturnsValidIndexNameForComplexIndex()
        {
            var expr = new CreateIndexExpression()
            {
                Index =
                {
                    TableName = "Bacon",
                    Columns =
                    {
                        new IndexColumnDefinition { Name = "BaconName", Direction = Direction.Ascending },
                        new IndexColumnDefinition { Name = "BaconSpice", Direction = Direction.Descending }
                    }
                }
            };

            var processed = expr.Apply(ConventionSets.NoSchemaName);

            processed.Index.Name.ShouldBe("IX_Bacon_BaconName_BaconSpice");
        }

        [Test]
        public void TypeIsMigrationReturnsTrueIfTypeExtendsMigrationAndHasMigrationAttribute()
        {
            _default.TypeIsMigration(typeof(DefaultConventionMigrationFake))
                .ShouldBeTrue();
        }

        [Test]
        public void TypeIsMigrationReturnsFalseIfTypeDoesNotExtendMigration()
        {
            _default.TypeIsMigration(typeof(object))
                .ShouldBeFalse();
        }

        [Test]
        public void TypeIsMigrationReturnsFalseIfTypeDoesNotHaveMigrationAttribute()
        {
            _default.TypeIsMigration(typeof(MigrationWithoutAttributeFake))
                .ShouldBeFalse();
        }

        [Test]
        public void GetMaintenanceStageReturnsCorrectStage()
        {
            _default.GetMaintenanceStage(typeof (MaintenanceAfterEach))
                .ShouldBe(MigrationStage.AfterEach);
        }

        [Test]
        public void MigrationInfoShouldRetainMigration()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = _default.GetMigrationInfoForMigration(migration);
            migrationinfo.Migration.GetType().ShouldBeSameAs(migration.GetType());
        }

        [Test]
        public void MigrationInfoShouldExtractVersion()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = _default.GetMigrationInfoForMigration(migration);
            migrationinfo.Version.ShouldBe(123);
        }

        [Test]
        public void MigrationInfoShouldExtractTransactionBehavior()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = _default.GetMigrationInfoForMigration(migration);
            migrationinfo.TransactionBehavior.ShouldBe(TransactionBehavior.None);
        }

        [Test]
        public void MigrationInfoShouldExtractTraits()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = _default.GetMigrationInfoForMigration(migration);
            migrationinfo.Trait("key").ShouldBe("test");
        }

        [Test]
        public void DefaultSchemaConventionDefaultsToNull()
        {
            var expr = new ConventionsTestClass();
            var processed = ConventionSets.NoSchemaName.SchemaConvention.Apply(expr);
            processed.SchemaName.ShouldBeNull();
        }

        [Test]
        public void TypeHasTagsReturnTrueIfTypeHasTagsAttribute()
        {
            _default.TypeHasTags(typeof(TaggedWithUk))
                .ShouldBeTrue();
        }

        [Test]
        public void TypeHasTagsReturnTrueIfInheritedTypeHasTagsAttribute()
        {
            _default.TypeHasTags(typeof(InheritedFromTaggedWithUk))
                .ShouldBeTrue();
        }

        [Test]
        public void TypeHasTagsReturnFalseIfTypeDoesNotHaveTagsAttribute()
        {
            _default.TypeHasTags(typeof(HasNoTagsFake))
                .ShouldBeFalse();
        }

        [Test]
        public void TypeHasTagsReturnTrueIfBaseTypeDoesHaveTagsAttribute()
        {
            _default.TypeHasTags(typeof(ConcretehasTagAttribute))
                .ShouldBeTrue();
        }

        public class TypeHasMatchingTags
        {
            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagAttributeButNoTagsPassedInReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUk), new string[] { })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagAttributeWithNoTagNamesReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(HasTagAttributeWithNoTagNames), new string[] { })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesNotMatchSingleThenTagReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "IE" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesMatchSingleTagThenReturnsTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatPartiallyMatchesTagThenReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK2" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesMatchMultipleTagsThenReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK", "Production" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributeThatDoesMatchSingleTagThenReturnsTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributesThatDoesMatchMultipleTagsThenReturnsTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK", "Production" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInOneAttributeThatDoesMatchMultipleTagsThenReturnsTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInOneTagsAttribute), new[] { "UK", "Production" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributesThatDontNotMatchMultipleTagsThenReturnsFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK", "IE" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenBaseTypeHasTagsThenConcreteTypeReturnsTrue()
            {
                _default.TypeHasMatchingTags(typeof(ConcretehasTagAttribute), new[] { "UK" })
                    .ShouldBeTrue();
            }


            //new
            [Test]
            [Category("Tagging")]
            public void WhenTypeHasSingleTagWithSingleTagNameAndBehaviorOfAnyAndHasMatchingTagNamesThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUkAndAnyBehavior), new[] { "UK", "IE" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasSingleTagWithSingleTagNameAndBehaviorOfAnyButNoMatchingTagNamesThenReturnFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithUkAndAnyBehavior), new[] { "Chrome", "IE" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasSingleTagWithMultipleTagNamesAndBehaviorOfAnyWithSomeMatchingTagNamesThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingAndAnyBehaviorInOneTagsAttribute), new[] { "UK", "Staging", "IE" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasSingleTagWithMultipleTagNamesAndBehaviorOfAnyWithNoMatchingTagNamesThenReturnFalse()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingAndAnyBehaviorInOneTagsAttribute), new[] { "IE", "Chrome" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasMultipleTagsWithMultipleTagNamesAndAllTagsHaveBehaviorOfAnyWithAllHavingAMatchingTagNameThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributesWithAnyBehaviorOnBoth), new[] { "UK", "Staging" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasMultipleTagsWithMultipleTagNamesAndAllTagsHaveBehaviorOfAnyWithOneTagNotHavingAMatchingTagNameThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributesWithAnyBehaviorOnBoth), new[] { "UK", "IE" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasMultipleTagsWithMultipleTagNamesAndOneHasBehaviorOfAnyAndOtherHasBehaviorOfAllWithAllTagNamesMatchingThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndAllBehaviorAndProductionAndStagingAndAnyBehaviorInTwoTagsAttributes), new[] { "UK", "Staging" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasMultipleTagsWithMultipleTagNamesAndOneHasBehaviorOfAnyAndOtherHasBehaviorOfAllWithoutAllTagNamesMatchingThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndAllBehaviorAndProductionAndStagingAndAnyBehaviorInTwoTagsAttributes), new[] { "UK", "Staging", "IE" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasMultipleTagsWithMultipleTagNamesAndOneHasBehaviorOfAnyWithoutAnyMatchingTagNamesAndOtherHasBehaviorOfAllWithTagNamesMatchingThenReturnTrue()
            {
                _default.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndAllBehaviorAndProductionAndStagingAndAnyBehaviorInTwoTagsAttributes), new[] { "BE", "UK" })
                    .ShouldBeTrue();
            }
        }

        [Test]
        public void GetAutoScriptUpName()
        {
            var processor = new Mock<IMigrationProcessor>();
            processor.SetupGet(p => p.DatabaseType).Returns("SqlServer2016");
            processor.SetupGet(p => p.DatabaseTypeAliases).Returns(new[] { "SqlServer" });
            var serviceProvider = ServiceCollectionExtensions.CreateServices()
                .WithProcessor(processor)
                .AddScoped<IConnectionStringReader>(_ => new PassThroughConnectionStringReader("No connection"))
                .BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<IMigrationContext>();
            var expr = new AutoScriptMigrationFake();
            expr.GetUpExpressions(context);

            var expression = context.Expressions.Single();
            var processed = (IAutoNameExpression)expression.Apply(ConventionSets.NoSchemaName);
            processed.AutoNames.ShouldNotBeNull();
            CollectionAssert.AreEqual(
                new[]
                {
                    "Scripts.Up.20130508175300_AutoScriptMigrationFake_SqlServer2016.sql",
                    "Scripts.Up.20130508175300_AutoScriptMigrationFake_SqlServer.sql",
                    "Scripts.Up.20130508175300_AutoScriptMigrationFake_Generic.sql",
                },
                processed.AutoNames);
        }

        [Test]
        public void GetAutoScriptDownName()
        {
            var processor = new Mock<IMigrationProcessor>();
            processor.SetupGet(p => p.DatabaseType).Returns("SqlServer2016");
            processor.SetupGet(p => p.DatabaseTypeAliases).Returns(new[] { "SqlServer" });
            var serviceProvider = ServiceCollectionExtensions.CreateServices()
                .WithProcessor(processor)
                .AddScoped<IConnectionStringReader>(_ => new PassThroughConnectionStringReader("No connection"))
                .BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<IMigrationContext>();
            var expr = new AutoScriptMigrationFake();
            expr.GetDownExpressions(context);

            var expression = context.Expressions.Single();
            var processed = (IAutoNameExpression)expression.Apply(ConventionSets.NoSchemaName);

            processed.AutoNames.ShouldNotBeNull();
            CollectionAssert.AreEqual(
                new[]
                {
                    "Scripts.Down.20130508175300_AutoScriptMigrationFake_SqlServer2016.sql",
                    "Scripts.Down.20130508175300_AutoScriptMigrationFake_SqlServer.sql",
                    "Scripts.Down.20130508175300_AutoScriptMigrationFake_Generic.sql",
                },
                processed.AutoNames);
        }

        private class ConventionsTestClass : ISchemaExpression, IFileSystemExpression
        {
            public string SchemaName { get; set; }
            public string RootPath { get; set; }
        }
    }

    [Migration(20130508175300)]
    class AutoScriptMigrationFake : AutoScriptMigration
    {
        public AutoScriptMigrationFake()
            : base(new DefaultEmbeddedResourceProvider())
        {
        }
    }

    [Tags("BE", "UK", "Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingInOneTagsAttribute
    {
    }

    [Tags(TagBehavior.RequireAny, "BE", "UK", "Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingAndAnyBehaviorInOneTagsAttribute
    {
    }

    [Tags("BE", "UK")]
    [Tags("Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes
    {
    }

    [Tags(TagBehavior.RequireAny, "BE", "UK")]
    [Tags(TagBehavior.RequireAny, "Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributesWithAnyBehaviorOnBoth
    {
    }

    [Tags(TagBehavior.RequireAll,"BE", "UK", "Staging")]
    [Tags(TagBehavior.RequireAny, "Staging", "Production")]
    public class TaggedWithBeAndUkAndAllBehaviorAndProductionAndStagingAndAnyBehaviorInTwoTagsAttributes
    {
    }

    [Tags("UK")]
    public class TaggedWithUk
    {
    }

    public class InheritedFromTaggedWithUk : TaggedWithUk
    {
    }

    [Tags(TagBehavior.RequireAny, "UK")]
    public class TaggedWithUkAndAnyBehavior
    {
    }

    [Tags]
    public class HasTagAttributeWithNoTagNames
    {
    }

    public class HasNoTagsFake
    {
    }

    [Tags("UK")]
    public abstract class BaseHasTagAttribute : Migration
    { }

    public class ConcretehasTagAttribute : BaseHasTagAttribute
    {
        public override void Up(){}

        public override void Down(){}
    }



    [Migration(123, TransactionBehavior.None)]
    [MigrationTrait("key", "test")]
    internal class DefaultConventionMigrationFake : Migration
    {
        public override void Up() { }
        public override void Down() { }
    }

    internal class MigrationWithoutAttributeFake : Migration
    {
        public override void Up() { }
        public override void Down() { }
    }

    [Maintenance(MigrationStage.AfterEach)]
    internal class MaintenanceAfterEach : Migration
    {
        public override void Up() { }
        public override void Down() { }
    }
}
