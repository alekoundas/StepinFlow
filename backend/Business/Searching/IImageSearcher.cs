using System.Drawing;

namespace Business.Searching
{
    public interface IImageSearcher
    {
        ImageSearchResult Search(Rectangle bounds, SearchSettings settings, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit);
    }
}
