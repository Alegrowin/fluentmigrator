#region License
// Copyright (c) 2007-2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Generators.Hana;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.Hana;

using NUnit.Framework;

using Sap.Data.Hana;

namespace FluentMigrator.Tests.Integration.Processors.Hana.EndToEnd
{
    [TestFixture]
    [Category("Integration")]
    [Category("Hana")]
    public class TestRollbackColumnCreation : HanaEndToEndFixture
    {
        public HanaConnection Connection { get; set; }

        public HanaProcessor Processor { get; set; }

        [SetUp]
        public void SetUp()
        {
            if (!IntegrationTestOptions.Hana.IsEnabled)
                Assert.Ignore();
            Connection = new HanaConnection(IntegrationTestOptions.Hana.ConnectionString);
            Processor = new HanaProcessor(Connection, new HanaGenerator(), new TextWriterAnnouncer(TestContext.Out), new ProcessorOptions(), new HanaDbFactory());
            Connection.Open();
            Processor.BeginTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            Processor?.CommitTransaction();
            Processor?.Dispose();
        }

        [Test]
        public void Delete_ColumnCreateOnTableWithExplicitPk_ColumnShouldBeDropped()
        {
            DeleteTableIfExists("Teste", "Teste1");

            Migrate(typeof(ImplicitlyCreatedFkForHana.CreateImplicitFk).Namespace);
        }

        private void DeleteTableIfExists(params string[] tableNames)
        {
            foreach (var tableName in tableNames)
            {
                if (Processor.TableExists(null, tableName))
                    Processor.Execute(string.Format("DROP TABLE \"{0}\"", tableName));
            }

        }
    }

    namespace ImplicitlyCreatedFkForHana
    {
        [Migration(1)]
        public class CreateImplicitFk : Migration
        {
            public override void Up()
            {

                Create.Table("Teste1")
                    .WithColumn("Id").AsInt32().PrimaryKey("PK_TST").Identity()
                    .WithColumn("Nome").AsString(100);

                Create.Table("Teste")
                    .WithColumn("Id").AsInt32().PrimaryKey()
                    .ForeignKey("Teste1", "Id")
                    .WithColumn("Nome").AsString();

                Delete.PrimaryKey("").FromTable("Teste");
            }

            public override void Down()
            {
                Delete.Table("Teste");
                Delete.Table("Teste1");
            }
        }
    }
}
