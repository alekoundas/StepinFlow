using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.Models
{
    /// <summary>
    /// Something a person did, with every recorded input it was made of, in the order recorded. Its
    /// first input is where it stands in the recording and when it started; its last is when it ended.
    /// </summary>
    public sealed class HumanAction
    {
        public RecordedActionDto Action { get; }

        public IReadOnlyList<RecordedInput> Inputs { get; }

        public HumanAction(RecordedActionDto action, IReadOnlyList<RecordedInput> inputs)
        {
            if (inputs.Count == 0)
                throw new ArgumentException("An action is made of at least one input.", nameof(inputs));

            Action = action;
            Inputs = inputs.ToList();
        }
    }
}
