using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// An import or a recording saved its Success and Failure rows with no name. They are named for
    /// their type, and a branch that has a name keeps it.
    /// </summary>
    public sealed class NameBranchRowsMigrationTests : IDisposable
    {
        private const string BEFORE = "20261029145802_AiLocalModel";
        private const string AFTER = "20261031201705_NameBranchRows";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void A_branch_with_no_name_is_named_for_its_type()
        {
            int searchId = SearchSavedBefore(successName: "", failureName: "");

            BranchNames(searchId).ShouldBe(["Success", "Failure"]);
        }

        [Fact]
        public void A_branch_with_a_name_keeps_it()
        {
            int searchId = SearchSavedBefore(successName: "Found it", failureName: "");

            BranchNames(searchId).ShouldBe(["Found it", "Failure"]);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private int SearchSavedBefore(string successName, string failureName)
        {
            MigrateTo(BEFORE);

            int searchId;
            using (AppDbContext db = _database.Open())
            {
                Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
                db.Flows.Add(flow);
                db.SaveChanges();

                FlowStep search = new FlowStep { FlowId = flow.Id, RootId = flow.Id, Name = "Find", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE };
                db.FlowSteps.Add(search);
                db.FlowSteps.Add(new FlowStep { RootId = flow.Id, ParentFlowStep = search, FlowStepType = FlowStepTypeEnum.SUCCESS, Name = successName, OrderNumber = 0 });
                db.FlowSteps.Add(new FlowStep { RootId = flow.Id, ParentFlowStep = search, FlowStepType = FlowStepTypeEnum.FAILURE, Name = failureName, OrderNumber = 1 });
                db.SaveChanges();

                searchId = search.Id;
            }

            MigrateTo(AFTER);

            return searchId;
        }

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private List<string> BranchNames(int searchId)
        {
            using AppDbContext db = _database.Open();

            return db.FlowSteps
                .Where(x => x.ParentFlowStepId == searchId)
                .OrderBy(x => x.OrderNumber)
                .Select(x => x.Name)
                .ToList();
        }
    }
}
