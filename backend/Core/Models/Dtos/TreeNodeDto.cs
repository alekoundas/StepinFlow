using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class TreeNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }
        public bool HasChildren { get; set; }
        public IEnumerable<TreeNodeDto> Children { get; set; } = new Collection<TreeNodeDto>();
    }
}
