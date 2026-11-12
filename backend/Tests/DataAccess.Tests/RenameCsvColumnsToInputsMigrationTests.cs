using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class RenameCsvColumnsToInputsMigrationTests : IDisposable
    {
        private const string BEFORE = "20261107231538_OcrLanguageIsReader";
        private const string AFTER = "20261112091311_RenameCsvColumnsToInputs";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        // The real database has three: phone_number, input and input_2, a secret. Each column a
        // distinct value, so a column moved into another would show.
        [Fact]
        public void Every_input_comes_along_with_its_id_and_values()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int phone = SavedColumn(flowId, "phone_number", "555-01", false, 3);
            int secret = SavedColumn(flowId, "input_2", string.Empty, true, 7);
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            FlowInput first = db.FlowInputs.Single(x => x.Id == phone);
            first.Name.ShouldBe("phone_number");
            first.DefaultValue.ShouldBe("555-01");
            first.IsSecret.ShouldBeFalse();
            first.OrderNumber.ShouldBe(3);
            first.FlowId.ShouldBe(flowId);
            first.SecretValue.ShouldBeEmpty();

            FlowInput second = db.FlowInputs.Single(x => x.Id == secret);
            second.Name.ShouldBe("input_2");
            second.IsSecret.ShouldBeTrue();
            second.OrderNumber.ShouldBe(7);
            second.SecretValue.ShouldBeEmpty();
        }

        // The unique index is new, and two rows a flow had under one name would stop it.
        [Fact]
        public void A_name_two_inputs_of_one_flow_shared_is_numbered_on_the_later_one()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int first = SavedColumn(flowId, "email", "a@example.com", false, 0);
            int later = SavedColumn(flowId, "email", "b@example.com", false, 1);
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowInputs.Single(x => x.Id == first).Name.ShouldBe("email");
            db.FlowInputs.Single(x => x.Id == later).Name.ShouldBe("email " + later);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Sign up", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        // The table is older than the model, so written as SQL.
        private int SavedColumn(int flowId, string name, string defaultValue, bool isSecret, int orderNumber)
        {
            using AppDbContext db = _database.Open();

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowCsvColumns (Name, DefaultValue, IsSecret, OrderNumber, FlowId, CreatedOn)
                VALUES ({0}, {1}, {2}, {3}, {4}, '2026-11-01');", name, defaultValue, isSecret, orderNumber, flowId);

            return db.Database
                .SqlQueryRaw<int>("SELECT MAX(Id) AS Value FROM FlowCsvColumns")
                .AsEnumerable()
                .Single();
        }
    }
}
