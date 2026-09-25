using Core.Models.Business;
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

        public IReadOnlyList<IReadOnlyList<int>> GroupSimilar(IReadOnlyList<byte[]> images)
        {
            throw new NotImplementedException();
        }

        public byte[]? FlattenErasedPixels(byte[] png)
        {
            throw new NotImplementedException();
        }

        public byte[] Downscale(byte[] jpeg, int maxEdge, int quality)
        {
            throw new NotImplementedException();
        }
    }
}
