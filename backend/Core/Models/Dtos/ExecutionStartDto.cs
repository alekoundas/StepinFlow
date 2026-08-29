using Core.Enums;

namespace Core.Models.Dtos
{
    public class ExecutionStartDto
    {
        public int FlowId { get; set; }

        public ExecutionHistoryLevelEnum HistoryLevel { get; set; } = ExecutionHistoryLevelEnum.STEPS;
        public List<int> Breakpoints { get; set; } = new List<int>();
    }
}
