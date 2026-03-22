using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class TreeNodeDto
    {
        public int Id { get; set; }
        public int Key { get; set; }
        public  bool Droppable { get; set; }
        public  bool Draggable { get; set; }
        public  bool Selectable { get; set; }
        public bool Leaf { get; set; } //Specifies if the node has children. Used in lazy loading.

        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum flowStepType { get; set; } = FlowStepTypeEnum.FAILURE;
        public int OrderNumber { get; set; }
        public bool isFlow { get; set; }

        public IEnumerable<TreeNodeDto> Children { get; set; } = new Collection<TreeNodeDto>();
    }
}
