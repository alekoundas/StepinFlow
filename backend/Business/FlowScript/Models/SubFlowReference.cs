namespace Business.FlowScript.Models
{
    /// <summary>
    /// The flow a <c>Sub Flow</c> line calls, as the file names it: by name, and by Id when the line
    /// has one. A line a person or a model wrote may leave the Id out, and a file from before the Id
    /// names a path instead, such as <c>flows/checkout.sflw</c>.
    /// </summary>
    public sealed class SubFlowReference
    {
        public string Name { get; set; } = string.Empty;

        public Guid? PublicId { get; set; }

        /// <summary>The line it was read from, for a warning about it. Zero when it was loaded rather than read.</summary>
        public int Line { get; set; }
    }
}
