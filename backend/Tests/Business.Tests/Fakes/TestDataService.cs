using Business.Executions;
using Business.Flows.DataService;
using Business.Flows.DataService.DataHandlers;
using Business.Flows.FlowValidationService;
using Business.Repository;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Business.Tests.Fakes
{
    /// <summary>The data services over a test database, wired the way the app wires them.</summary>
    public static class TestDataService
    {
        // Never made: deleting a flow looks here only for the folders its executions name.
        private static readonly string NO_HISTORY_FOLDER = Path.Combine(Path.GetTempPath(), "stepinflow-no-history");

        public static DataService For(
            IDbContextFactory<AppDbContext> database,
            IGitRepositoryService? gitRepositoryService = null,
            IExecutionEngine? executionEngine = null,
            string? historyFolder = null)
        {
            FlowValidationService validation = new FlowValidationService(new FakeOcrService());
            IGitRepositoryService repository = gitRepositoryService ?? new FakeGitRepositoryService();
            IExecutionEngine engine = executionEngine ?? new FakeExecutionEngine();
            ExecutionDeleteService executionDeleteService = new ExecutionDeleteService(database, engine, NullLogger<ExecutionDeleteService>.Instance, historyFolder ?? NO_HISTORY_FOLDER);

            return new DataService(
                new FlowDataService(database, validation, repository, engine, executionDeleteService),
                new FlowStepDataService(database, validation, repository),
                new FlowAreaDataService(database, validation, repository),
                new FlowPointDataService(database, validation, repository),
                new FlowInputDataService(database, validation, repository),
                new FlowTemplateDataService(database, validation, repository),
                new DiscordBotDataService(database));
        }
    }
}
