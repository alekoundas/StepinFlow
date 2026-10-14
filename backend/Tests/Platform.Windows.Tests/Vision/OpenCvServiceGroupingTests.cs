using Core.Models.Business.OpenCV;
using OpenCvSharp;
using Platform.Windows.Vision;

namespace Platform.Windows.Tests.Vision
{
    /// <summary>
    /// Clicked elements that are the same picture, on drawn screenshots: a 24 pixel icon on a row of
    /// a table, with that row's own text beside it.
    /// </summary>
    public sealed class OpenCvServiceGroupingTests
    {
        private const int ICON = 24;
        private const int AROUND = 6;

        private readonly OpenCvService _openCv = new OpenCvService();

        private static void Arrow(Mat icon)
        {
            Cv2.ArrowedLine(icon, new Point(12, 3), new Point(12, 20), Scalar.Black, 2);
        }

        private static void Cross(Mat icon)
        {
            Cv2.Line(icon, new Point(4, 4), new Point(19, 19), Scalar.Black, 2);
            Cv2.Line(icon, new Point(19, 4), new Point(4, 19), Scalar.Black, 2);
        }

        private static void Nothing(Mat icon)
        {
        }

        // The model's box is drawn `offset` pixels off the icon, as it can be from one screenshot to the next.
        private static ElementPicture Clicked(Action<Mat> draw, string rowText, int offset = 0)
        {
            using Mat screen = new Mat(new Size(240, 100), MatType.CV_8UC3, new Scalar(235, 235, 235));
            Cv2.PutText(screen, rowText, new Point(6, 56), HersheyFonts.HersheySimplex, 0.5, Scalar.Black);

            using (Mat icon = new Mat(screen, new Rect(160, 38, ICON, ICON)))
            {
                icon.SetTo(Scalar.White);
                draw(icon);
            }

            Rect element = new Rect(160 + offset, 38 + offset, ICON, ICON);
            Rect around = new Rect(element.X - AROUND, element.Y - AROUND, ICON + AROUND * 2, ICON + AROUND * 2);

            using Mat elementImage = new Mat(screen, element);
            using Mat aroundImage = new Mat(screen, around);

            return new ElementPicture { Element = elementImage.ImEncode(".png"), Around = aroundImage.ImEncode(".png") };
        }

        [Fact]
        public void The_same_icon_on_two_rows_is_one_group_even_with_its_box_a_few_pixels_off()
        {
            IReadOnlyList<IReadOnlyList<int>> groups = _openCv.GroupSameElements(
            [
                Clicked(Arrow, "INV-2041  1,240.00"),
                Clicked(Arrow, "INV-2042  860.50", offset: 3),
            ]);

            groups.ShouldHaveSingleItem().ShouldBe([0, 1]);
        }

        [Fact]
        public void Two_different_icons_side_by_side_are_not_grouped()
        {
            IReadOnlyList<IReadOnlyList<int>> groups = _openCv.GroupSameElements(
            [
                Clicked(Arrow, "Toolbar"),
                Clicked(Cross, "Toolbar"),
            ]);

            groups.ShouldBeEmpty();
        }

        [Fact]
        public void Only_the_ones_that_match_are_grouped()
        {
            IReadOnlyList<IReadOnlyList<int>> groups = _openCv.GroupSameElements(
            [
                Clicked(Arrow, "INV-2041"),
                Clicked(Cross, "INV-2042"),
                Clicked(Arrow, "INV-2043"),
            ]);

            groups.ShouldHaveSingleItem().ShouldBe([0, 2]);
        }

        // Flat colour matches every other flat colour.
        [Fact]
        public void A_flat_element_is_in_no_group()
        {
            IReadOnlyList<IReadOnlyList<int>> groups = _openCv.GroupSameElements(
            [
                Clicked(Nothing, "INV-2041"),
                Clicked(Nothing, "INV-2042"),
            ]);

            groups.ShouldBeEmpty();
        }
    }
}
