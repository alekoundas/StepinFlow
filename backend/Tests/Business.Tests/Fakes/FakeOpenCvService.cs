using Core.Models.Business.OpenCV;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Answers a match by the template's first byte, so a test says which templates are "on screen"
    /// with a one-byte image. Every request is kept, so a test can read the scale it was asked for.
    /// </summary>
    public sealed class FakeOpenCvService : IOpenCvService
    {
        private readonly Dictionary<byte, TemplateMatchOutcome> _answers = new Dictionary<byte, TemplateMatchOutcome>();

        public List<TemplateMatchRequest> Requests { get; } = new List<TemplateMatchRequest>();

        public FakeOpenCvService Answer(byte template, TemplateMatchOutcome outcome)
        {
            _answers[template] = outcome;
            return this;
        }

        public FakeOpenCvService Found(byte template, float score, int x = 0, int y = 0, float scale = 1f)
        {
            return Answer(template, new TemplateMatchOutcome
            {
                Matches = [new TemplateMatchResult { X = x, Y = y, Width = 10, Height = 10, Score = score, Scale = scale }],
            });
        }

        public FakeOpenCvService NotFound(byte template, float bestScore)
        {
            return Answer(template, new TemplateMatchOutcome
            {
                Rejected = [new TemplateMatchResult { Width = 10, Height = 10, Score = bestScore, Scale = 1f }],
            });
        }

        public TemplateMatchOutcome Match(TemplateMatchRequest request)
        {
            Requests.Add(request);

            if (_answers.TryGetValue(request.TemplateImage[0], out TemplateMatchOutcome? outcome))
                return outcome;

            return new TemplateMatchOutcome();
        }

        /// <summary>Every call's elements, in order.</summary>
        public List<IReadOnlyList<ElementPicture>> Grouped { get; } = new List<IReadOnlyList<ElementPicture>>();

        // Everything passed in at once is the same picture, unless a test says otherwise.
        public Func<IReadOnlyList<ElementPicture>, IReadOnlyList<IReadOnlyList<int>>>? Groups { get; set; }

        public IReadOnlyList<IReadOnlyList<int>> GroupSameElements(IReadOnlyList<ElementPicture> elements)
        {
            Grouped.Add(elements);

            if (Groups != null)
                return Groups(elements);

            if (elements.Count < 2)
                return [];

            List<int> all = Enumerable.Range(0, elements.Count).ToList();
            return [all];
        }

        public byte[]? FlattenErasedPixels(byte[] png)
        {
            throw new NotImplementedException();
        }

        /// <summary>The longest edge each picture was asked down to. The picture comes back as it was.</summary>
        public List<int> DownscaledTo { get; } = new List<int>();

        public byte[] Downscale(byte[] jpeg, int maxEdge, int quality)
        {
            DownscaledTo.Add(maxEdge);
            return jpeg;
        }

        public byte[]? Thumbnail(byte[] png, int maxEdge)
        {
            throw new NotImplementedException();
        }
    }
}
