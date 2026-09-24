using Core.Models.Business;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)


    /// <summary>
    /// The image comparisons the application needs. Encoded bytes in, answers out: nothing here hands back a native image handle, so
    /// a caller cannot start assembling its own pipeline and end up owning half the matching.
    /// </summary>
    public interface IOpenCvService
    {
        /// <summary>
        /// Every match at or above the threshold, best first, and how close the screenshot came when none of them cleared it.
        /// </summary>
        TemplateMatchOutcome Match(TemplateMatchRequest request);

        /// <summary>
        /// Groups the images that show the same thing, as indices into what was passed in.
        ///
        /// One call rather than a comparison the caller loops over, so the images are decoded once
        /// instead of once per pair, and so the thresholds that decide "the same thing" stay with
        /// the matching rather than leaking into whatever is asking.
        ///
        /// An image too flat to compare is left out entirely: flat colour matches all other flat
        /// colour, and a group built on that is a coincidence.
        /// </summary>
        IReadOnlyList<IReadOnlyList<int>> GroupSimilar(IReadOnlyList<byte[]> images);

        /// <summary>
        /// Fully erased pixels turned white, so a template image can be shown to a model that has
        /// been told what white means. Null when the bytes are not a readable image.
        ///
        /// Stays PNG: a template is all hard edges and JPEG rings around them.
        /// </summary>
        byte[]? FlattenErasedPixels(byte[] png);

        /// <summary>
        /// Re-encodes so the longest edge is at most <paramref name="maxEdge"/>, returning the
        /// original when it already is. How large is the caller's decision; doing it is not.
        /// </summary>
        byte[] Downscale(byte[] jpeg, int maxEdge, int quality);
    }
}
