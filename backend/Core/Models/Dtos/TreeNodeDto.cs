using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class TreeNodeDto
    {
        // PrimeReact TreeNode fields
        //
        // Key must be unique across the whole tree, and Flow ids and FlowStep ids are separate
        // sequences: a raw id would make Flow 5 and FlowStep 5 the same node as far as selection
        // and expansion are concerned. Callers read EntityId, never parse Key.
        // (Not named Id: PrimeReact's own TreeNode already declares a string id.)
        public string Key { get; set; } = string.Empty; // PrimeReact expects PK in string.
        public int EntityId { get; set; }
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

        /// <summary>
        /// Tree key for an entity id. Must stay in sync with buildTreeNodeKey on the frontend.
        /// </summary>
        public static string BuildKey(int id, bool isFlow) => isFlow ? $"flow-{id}" : $"step-{id}";
    }
}
