namespace Core.Models.Business
{
    /// <summary>
    /// One picture handed to a model, with the sentence that says what it is.
    ///
    /// The label is not decoration. Two screenshots and a template image are three pictures
    /// unless something says which is which, and a model given three unlabelled ones describes
    /// them instead of comparing them.
    /// </summary>
    public class AiImage
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The same picture in a few words, for the reader rather than the model. The label is a
        /// sentence because a model needs telling what it is looking at; a chip in the chat has
        /// room for "Screen at Image Search" and no more.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public byte[] Bytes { get; set; } = [];

        /// <summary>Screenshots are jpeg; a template image is png, so its edges stay crisp.</summary>
        public string MediaType { get; set; } = "image/jpeg";
    }
}
