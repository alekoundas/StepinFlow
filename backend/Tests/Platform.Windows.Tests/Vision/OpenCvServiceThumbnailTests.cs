using OpenCvSharp;
using Platform.Windows.Vision;

namespace Platform.Windows.Tests.Vision
{
    /// <summary>
    /// The small copy of a template a tree row shows.
    /// Test:
    /// 1) A large template shrinks to the edge asked for, keeping its shape and its erased pixels
    /// 2) A template already that small comes back as it was
    /// 3) Bytes that are not an image give no thumbnail
    /// </summary>
    public sealed class OpenCvServiceThumbnailTests
    {
        private const int EDGE = 96;

        private readonly OpenCvService _openCv = new OpenCvService();

        // Opaque grey with the top left quarter erased, as the editor's eraser leaves it.
        private static byte[] Template(int width, int height)
        {
            using Mat picture = new Mat(height, width, MatType.CV_8UC4, new Scalar(90, 120, 150, 255));
            picture[new Rect(0, 0, width / 2, height / 2)].SetTo(Scalar.All(0));

            return picture.ImEncode(".png");
        }

        [Fact]
        public void A_large_template_shrinks_to_the_edge_keeping_its_shape_and_erased_pixels()
        {
            byte[] template = Template(400, 200);

            byte[]? thumbnail = _openCv.Thumbnail(template, EDGE);

            thumbnail.ShouldNotBeNull();
            using Mat decoded = Cv2.ImDecode(thumbnail, ImreadModes.Unchanged);
            decoded.Width.ShouldBe(EDGE);
            decoded.Height.ShouldBe(EDGE / 2);
            decoded.Channels().ShouldBe(4);
            decoded.At<Vec4b>(0, 0).Item3.ShouldBe((byte)0);
            decoded.At<Vec4b>(decoded.Height - 1, decoded.Width - 1).Item3.ShouldBe((byte)255);
        }

        [Fact]
        public void A_template_already_that_small_comes_back_as_it_was()
        {
            byte[] template = Template(60, 30);

            byte[]? thumbnail = _openCv.Thumbnail(template, EDGE);

            thumbnail.ShouldBeSameAs(template);
        }

        [Fact]
        public void Bytes_that_are_not_an_image_give_no_thumbnail()
        {
            byte[]? thumbnail = _openCv.Thumbnail([1, 2, 3], EDGE);

            thumbnail.ShouldBeNull();
        }
    }
}
