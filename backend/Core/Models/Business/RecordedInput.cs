using Core.Enums;

namespace Core.Models.Business
{
    public class RecordedInput
    {
        public RecordedInputTypeEnum Type { get; set; }
        public int PsysicalX { get; set; }
        public int PsysicalY { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public DateTime CreatedOn { get; set; }  = DateTime.Now;
    }
}
