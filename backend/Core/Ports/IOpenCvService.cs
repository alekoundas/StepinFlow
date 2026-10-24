using Core.Models.Business.OpenCV;

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
        /// Groups the elements that are the same picture, as indices into what was passed in. Only
        /// groups of two or more.
        ///
        /// Each element is looked for in the other's surroundings, both ways, so a box drawn a few
        /// pixels off the other still matches, and neither carries background into the comparison.
        ///
        /// One call rather than a comparison the caller loops over, so the images are decoded once
        /// instead of once per pair, and so the thresholds that decide "the same picture" stay with
        /// the matching rather than leaking into whatever is asking.
        ///
        /// An element too flat to compare is in no group: flat colour matches all other flat colour,
        /// and a group built on that is a coincidence.
        /// </summary>
        IReadOnlyList<IReadOnlyList<int>> GroupSameElements(IReadOnlyList<ElementPicture> elements);

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

        /// <summary>
        /// A template image small enough for a list to show, its longest edge at most
        /// <paramref name="maxEdge"/>. Stays PNG with its erased pixels; the original when it is
        /// already that small, null when the bytes are not a readable image.
        /// </summary>
        byte[]? Thumbnail(byte[] png, int maxEdge);
    }
}
