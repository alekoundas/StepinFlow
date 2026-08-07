namespace Core.Models.Dtos
{
    /// <summary>
    /// Identifies the node whose children the tree wants.
    ///
    /// Flow ids and FlowStep ids are separate sequences, so the id alone is ambiguous: without
    /// IsFlow the query cannot tell "root steps of Flow 5" from "children of FlowStep 5".
    /// </summary>
    public class TreeNodeRequestDto
    {
        public int Id { get; set; }
        public bool IsFlow { get; set; }
    }
}
