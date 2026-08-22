namespace Core.Models
{
    public class BaseDbModel
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Stamped centrally by AppDbContext.SaveChanges on anything modified, so no handler has to remember.  
        /// </summary>
        public DateTime? UpdatedOn { get; set; }
    }
}
