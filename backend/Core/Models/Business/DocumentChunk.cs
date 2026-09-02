namespace Core.Models.Business
{
    /// <summary>
    /// One retrievable piece of a document.
    ///
    /// Sized to be answerable on its own: a chunk that needs the paragraph above it to make sense
    /// is a chunk that will be retrieved without it.
    /// </summary>
    public sealed record DocumentChunk(
        string Source,
        string Title,
        string Heading,
        string Text,
        int Ordinal)
    {
        /// <summary>What to show when citing this, which reads better than a file path.</summary>
        public string Citation => string.IsNullOrEmpty(Heading) ? Title : $"{Title} > {Heading}";
    }
}
