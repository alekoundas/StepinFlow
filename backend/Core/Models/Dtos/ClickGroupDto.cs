namespace Core.Models.Dtos
{
    /// <summary>Clicks that hit the same picture: one template finds every one of them.</summary>
    public class ClickGroupDto
    {
        /// <summary>The clicks and drags in it, by their index among the recording's actions, in order.</summary>
        public List<int> ActionIndexes { get; set; } = [];

        /// <summary>In different places - the same icon on several rows - rather than one place clicked again.</summary>
        public bool IsMoving { get; set; }
    }
}
