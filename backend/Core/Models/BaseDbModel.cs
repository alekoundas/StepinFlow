namespace Core.Models
{
    public class BaseDbModel
    {
        public int Id { get; set; }
        /// <summary>Stamped by TimestampInterceptor on the way to the database.</summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>Stamped by TimestampInterceptor on the way to the database.</summary>
        public DateTime? UpdatedOn { get; set; }
    }
}
