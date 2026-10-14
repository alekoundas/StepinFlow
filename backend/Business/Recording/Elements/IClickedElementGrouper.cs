using Business.Recording.Models.Session;
using Core.Models.Dtos;

namespace Business.Recording.Elements
{
    public interface IClickedElementGrouper
    {
        /// <summary>
        /// The clicks that hit the same picture, from the elements the model found. A group is what
        /// one template would find every click of. onProgress is told the elements done and the
        /// elements there are.
        /// </summary>
        IReadOnlyList<ClickGroupDto> Group(
            IReadOnlyList<ClickedElementDto> elements,
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
