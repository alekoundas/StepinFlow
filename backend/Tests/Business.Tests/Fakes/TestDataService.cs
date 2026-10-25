using Business.Flows.DataService;
using Business.Flows.DataService.DataHandlers;
using Business.Flows.FlowValidationService;
using Business.Repository;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Tests.Fakes
{
    /// <summary>The data services over a test database, wired the way the app wires them.</summary>
    public static class TestDataService
    {
        public static DataService For(IDbContextFactory<AppDbContext> database, IGitRepositoryService? gitRepositoryService = null)
        {
            FlowValidationService validation = new FlowValidationService();
            IGitRepositoryService repository = gitRepositoryService ?? new FakeGitRepositoryService();

            return new DataService(
                new FlowDataService(database, validation, repository),
                new FlowStepDataService(database, validation, repository),
                new FlowAreaDataService(database, validation, repository),
                new FlowPointDataService(database, validation, repository),
                new FlowCsvColumnDataService(database, validation, repository),
                new FlowTemplateDataService(database, validation, repository));
        }
    }
}
