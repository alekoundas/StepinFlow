namespace Business.Recording.Generation.Models
{
    /// <summary>What the code took from a model's words, and what it left out and why.</summary>
    public sealed class AppliedWords
    {
        public List<string> Notes { get; } = [];
        public List<string> Questions { get; } = [];

        /// <summary>What was not taken, each in a sentence: a name too long, a check for words no screenshot showed.</summary>
        public List<string> NotTaken { get; } = [];
    }
}
