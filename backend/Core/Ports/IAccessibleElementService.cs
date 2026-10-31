using System.Drawing;
using Core.Models.Business;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface IAccessibleElementService
    {
        /// <summary>
        /// The element at a point on the screen, in physical pixels, as its application describes it.
        /// Null when nothing answers. An application that draws its own pixels - a game - answers
        /// with its window at most.
        /// </summary>
        AccessibleElement? ElementAt(Point point);
    }
}
