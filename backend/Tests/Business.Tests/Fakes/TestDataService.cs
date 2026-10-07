using Business.Flows.DataService;
using Business.Flows.DataService.DataHandlers;
using Business.Flows.FlowValidationService;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Tests.Fakes
{
    /// <summary>The data services over a test database, wired the way the app wires them.</summary>
    public static class TestDataService
    {
        public static DataService For(IDbContextFactory<AppDbContext> database)
        {
            FlowValidationService validation = new FlowValidationService();

            return new DataService(
                new FlowDataService(database, validation),
                new FlowStepDataService(database, validation),
                new FlowAreaDataService(database, validation),
                new FlowPointDataService(database, validation));
        }
    }
}
