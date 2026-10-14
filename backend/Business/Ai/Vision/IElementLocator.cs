using System.Drawing;
using Business.Ai.Vision.Models;
using Core.Models.Business;

namespace Business.Ai.Vision
{
    /// <summary>
    /// Finds the element under a click. Loaded for a batch of clicks and let go after: the model
    /// is hundreds of megabytes the app has no use for between recordings.
    /// </summary>
    public interface IElementLocator
    {
        /// <summary>Loads the detection model. False when it is not installed, and every element is then a box around the click.</summary>
        bool Load();

        /// <summary>The element under the point, both in the image's pixels.</summary>
        LocatedElement Locate(RawImage image, Point point, int dpi);

        void Unload();
    }
}
