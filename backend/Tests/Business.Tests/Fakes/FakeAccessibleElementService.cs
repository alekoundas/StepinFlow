using System.Drawing;
using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>The same element wherever it is asked, or none.</summary>
    public sealed class FakeAccessibleElementService : IAccessibleElementService
    {
        public AccessibleElement? Element { get; set; }

        public List<Point> Asked { get; } = new List<Point>();

        public AccessibleElement? ElementAt(Point point)
        {
            lock (Asked)
                Asked.Add(point);

            return Element;
        }
    }
}
