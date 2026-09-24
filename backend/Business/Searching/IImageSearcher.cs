using Core.Models.Business;

namespace Business.Searching
{
    public interface IImageSearcher
    {
        ImageSearchResult Search(AreaResolution area, SearchSettings settings, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit);
    }
}
