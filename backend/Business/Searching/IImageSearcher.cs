using Core.Models.Business.AreaPointResolution;

namespace Business.Searching
{
    public interface IImageSearcher
    {
        ImageSearchResult Search(AreaResolution area, SearchSettings settings, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit);
    }
}
