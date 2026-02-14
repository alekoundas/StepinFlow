namespace Core.Models
{
    public class BaseDbModel
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
