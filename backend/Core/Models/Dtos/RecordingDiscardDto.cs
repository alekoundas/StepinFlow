namespace Core.Models.Dtos
{
    public class RecordingDiscardDto
    {
        /// <summary>The flow the setup created, deleted with the recording while nothing else is in it.</summary>
        public int? FlowId { get; set; }
    }
}
