using System.Drawing;
using Business.Ai.Vision;
using Business.Ai.Vision.Models;
using Core.Enums;
using Core.Models.Business;

namespace Business.Tests.Fakes
{
    /// <summary>Finds a 40 by 20 element centred on every click, and remembers where it was asked.</summary>
    public sealed class FakeElementLocator : IElementLocator
    {
        public List<Point> Located { get; } = new List<Point>();
        public bool IsLoaded { get; private set; }
        public int UnloadCount { get; private set; }

        public bool Load()
        {
            IsLoaded = true;
            return true;
        }

        public LocatedElement Locate(RawImage image, Point point, int dpi)
        {
            Located.Add(point);

            return new LocatedElement
            {
                Bounds = new Rectangle(point.X - 20, point.Y - 10, 40, 20),
                FoundBy = ElementFoundByEnum.MODEL,
                Score = 0.9f,
            };
        }

        public void Unload()
        {
            IsLoaded = false;
            UnloadCount++;
        }
    }
}
