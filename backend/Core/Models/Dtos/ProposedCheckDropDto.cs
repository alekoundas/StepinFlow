namespace Core.Models.Dtos
{
    /// <summary>The Check page dropping a check a model added: the flow, the check, and the ones still undecided.</summary>
    public class ProposedCheckDropDto
    {
        public int FlowId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>The proposed checks not yet kept or dropped, by name, so the answer still offers them.</summary>
        public List<string> Undecided { get; set; } = [];
    }
}
