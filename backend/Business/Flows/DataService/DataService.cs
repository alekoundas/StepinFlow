using Business.Flows.DataService.DataHandlers;

namespace Business.Flows.DataService
{
    /// <summary>
    /// Handle every INSERT/UPDATE/DELETE queries and rules for all DB tables and reusable SELECT queries.
    /// </summary>
    public sealed class DataService
    {
        public FlowDataService Flow { get; }
        public FlowStepDataService FlowStep { get; }
        public FlowAreaDataService FlowArea { get; }
        public FlowPointDataService FlowPoint { get; }

        public DataService(FlowDataService flow, FlowStepDataService flowStep, FlowAreaDataService flowArea, FlowPointDataService flowPoint)
        {
            Flow = flow;
            FlowStep = flowStep;
            FlowArea = flowArea;
            FlowPoint = flowPoint;
        }
    }
}
