using Core.Enums;

namespace Core.Models.Business
{
    public class RecordedInput
    {
        public RecordedInputEnum Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public DateTime CreatedOn { get; set; }  = DateTime.Now;
    }
}
