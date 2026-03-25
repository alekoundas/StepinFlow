using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class TreeNodeDto
    {
        // PrimeReact TreeNode fields
        public string Key { get; set; } = string.Empty; // PrimeReact expects PK in string.
        public  bool Droppable { get; set; }
        public  bool Draggable { get; set; }
        public  bool Selectable { get; set; }
        public bool Leaf { get; set; } //Specifies if the node has children. // True doesnt allow expand

        // Custom values
        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum? flowStepType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsFlow { get; set; }
        public bool IsNew { get; set; }

        public int? ParentFlowId { get; set; }
        public int? ParentFlowStepId { get; set; }
        
        public IEnumerable<TreeNodeDto> Children { get; set; } = new Collection<TreeNodeDto>();
    }
}
