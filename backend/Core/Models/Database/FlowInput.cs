namespace Core.Models.Database
{
    /// <summary>A value the flow's steps type in, written {{name}}.</summary>
    public class FlowInput : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>The value, written into the script. Never set when IsSecret.</summary>
        public string DefaultValue { get; set; } = string.Empty;

        public bool IsSecret { get; set; }

        /// <summary>
        /// A secret's value. Only in the database: never in the script, a DTO or a request to a
        /// model, and only the execution reads it.
        /// </summary>
        public string SecretValue { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;
    }
}
