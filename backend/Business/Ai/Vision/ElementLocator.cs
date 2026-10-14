using System.Drawing;
using Business.Ai.Vision.Models;
using Core.Enums;
using Core.Models.Business;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Business.Ai.Vision
{
    /// <summary>
    /// The element under a click, from OmniParser's detection model (icon_detect_v3, MIT), on the
    /// cpu, locally.
    ///
    /// The model looks at 640 by 640 pixels around the click, at the screenshot's own pixels, so
    /// a small icon is seen at its real size; the whole screen would be six times the work for
    /// elements nobody clicked. When it has nothing to say - no model file, nothing under the
    /// click, or an element bigger than what it looked at - the element is a box around the click,
    /// and the note says which.
    ///
    /// One batch at a time: the recording session never looks for clicked elements twice at once.
    /// </summary>
    public sealed class ElementLocator : IElementLocator, IDisposable
    {
        private const float MINIMUM_SCORE = 0.25f;     // OmniParser's own defaults
        private const float MAXIMUM_OVERLAP = 0.7f;
        private const float CLICK_TOLERANCE = 3f;      // A click a pixel or two outside an element's edge is still on it.
        private const int EDGE_MARGIN = 2;
        private const int FALLBACK_SIZE = 64;          // At 96 dpi; scaled with the screen's.

        private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "icon_detect_v3.onnx");

        private InferenceSession? _session;


        // ================================================================
        // Public methods
        // ================================================================

        public bool Load()
        {
            if (_session != null)
                return true;

            if (!File.Exists(_modelPath))
                return false;

            try
            {
                _session = new InferenceSession(_modelPath);
                return true;
            }
            catch (Exception ex)
            {
                // A half downloaded file throws here. The box around the click still works.
                Console.Error.WriteLine($"[ElementLocator] Could not load {_modelPath}: {ex.Message}");
                _session = null;
                return false;
            }
        }

        public LocatedElement Locate(RawImage image, Point point, int dpi)
        {
            if (_session == null)
                return Fallback(image, point, dpi, "The element model is not installed, so this is a box around the click.");

            Rectangle region = RegionAround(image, point);
            DenseTensor<float> input = ElementDetector.BuildInput(image, region);
            List<NamedOnnxValue> inputs = [NamedOnnxValue.CreateFromTensor("images", input)];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run(inputs);
            List<Tensor<float>> tensors = outputs.Select(x => x.AsTensor<float>()).ToList();

            List<DetectedBox> found = ElementDetector.Decode(tensors, MINIMUM_SCORE);
            List<DetectedBox> boxes = ElementDetector.Suppress(found, MAXIMUM_OVERLAP);
            PointF inRegion = new PointF(point.X - region.X, point.Y - region.Y);
            DetectedBox? under = ElementDetector.Under(boxes, inRegion, CLICK_TOLERANCE);

            if (under == null)
                return Fallback(image, point, dpi, "The model found no element under the click, so this is a box around it.");

            if (IsCut(under.Bounds, region, image))
                return Fallback(image, point, dpi, "The element is bigger than the 640 pixels looked at around the click, so this is a box around it.");

            Rectangle bounds = Rectangle.FromLTRB(
                region.X + (int)MathF.Floor(under.Bounds.Left),
                region.Y + (int)MathF.Floor(under.Bounds.Top),
                region.X + (int)MathF.Ceiling(under.Bounds.Right),
                region.Y + (int)MathF.Ceiling(under.Bounds.Bottom));

            return new LocatedElement
            {
                Bounds = Rectangle.Intersect(bounds, new Rectangle(0, 0, image.Width, image.Height)),
                FoundBy = ElementFoundByEnum.MODEL,
                Score = under.Score,
            };
        }

        public void Unload()
        {
            _session?.Dispose();
            _session = null;
        }

        public void Dispose()
        {
            Unload();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // 640 by 640 centred on the point, moved back inside the image where it would leave it.
        // An image smaller than that is padded instead.
        private static Rectangle RegionAround(RawImage image, Point point)
        {
            int half = ElementDetector.INPUT_SIZE / 2;
            int x = Math.Clamp(point.X - half, 0, Math.Max(0, image.Width - ElementDetector.INPUT_SIZE));
            int y = Math.Clamp(point.Y - half, 0, Math.Max(0, image.Height - ElementDetector.INPUT_SIZE));

            return new Rectangle(x, y, ElementDetector.INPUT_SIZE, ElementDetector.INPUT_SIZE);
        }

        // Touching an edge of the region that is not an edge of the image: the element goes on
        // past what the model saw.
        private static bool IsCut(RectangleF box, Rectangle region, RawImage image)
        {
            bool isCutLeft = region.Left > 0 && box.Left <= EDGE_MARGIN;
            bool isCutTop = region.Top > 0 && box.Top <= EDGE_MARGIN;
            bool isCutRight = region.Right < image.Width && box.Right >= region.Width - EDGE_MARGIN;
            bool isCutBottom = region.Bottom < image.Height && box.Bottom >= region.Height - EDGE_MARGIN;

            return isCutLeft || isCutTop || isCutRight || isCutBottom;
        }

        private static LocatedElement Fallback(RawImage image, Point point, int dpi, string note)
        {
            int screenDpi = dpi > 0 ? dpi : 96;
            int size = FALLBACK_SIZE * screenDpi / 96;
            Rectangle box = new Rectangle(point.X - size / 2, point.Y - size / 2, size, size);

            return new LocatedElement
            {
                Bounds = Rectangle.Intersect(box, new Rectangle(0, 0, image.Width, image.Height)),
                FoundBy = ElementFoundByEnum.FALLBACK,
                Note = note,
            };
        }
    }
}
