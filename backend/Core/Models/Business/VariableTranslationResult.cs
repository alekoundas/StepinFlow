namespace Core.Models.Business
{
    /// <summary>What came back from swapping {{name}} for its value.</summary>
    public class VariableTranslationResult
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>Names nothing had a value for. Their braces are still in the text.</summary>
        public IReadOnlyList<string> Unresolved { get; set; } = [];

        public bool IsResolved
        {
            get { return Unresolved.Count == 0; }
        }
    }
}
