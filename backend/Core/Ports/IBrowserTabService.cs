using System.Drawing;
using Core.Models.Business;

namespace Core.Ports
{
    /// <summary>
    /// The tabs of a browser window, read the way a screen reader reads them: its address bar, its
    /// tab strip and the page in front.
    ///
    /// Only the front tab says its address, so finding one behind means bringing each to the front
    /// in turn. Which tab is wanted is <see cref="Helpers.TabAddressHelper"/>'s to decide.
    /// </summary>
    public interface IBrowserTabService
    {
        /// <summary>What the window's address bar says: its front tab's address. Null when there is none to read.</summary>
        string? FrontAddress(WindowHandle window);

        /// <summary>The page of the window's front tab, in physical pixels. Empty when there is none to read.</summary>
        Rectangle PageBounds(WindowHandle window);

        /// <summary>How many tabs the window has. Zero when its tab strip cannot be read.</summary>
        int TabCount(WindowHandle window);

        /// <summary>Where in the strip its front tab is, from zero. -1 when that cannot be read.</summary>
        int FrontTab(WindowHandle window);

        /// <summary>Brings a tab to the front, by where it is in the strip.</summary>
        bool SelectTab(WindowHandle window, int index);
    }
}
