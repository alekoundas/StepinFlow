using Core.Models.Business.OpenCV;
using Core.Models.Database;
using Core.Ports;

using DataAccess.Interceptors;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>
    /// A template's thumbnail follows its picture on the way to the database, whichever path saved it.
    /// Test:
    /// 1) A new picture is saved with one
    /// 2) A changed picture gets a new one
    /// 3) Moving the click point alone keeps it
    /// 4) A picture saved before thumbnails were made gets one the next time it is saved
    /// </summary>
    public sealed class TemplateThumbnailInterceptorTests : IDisposable
    {
        private static readonly byte[] Picture = [1, 2, 3];
        private static readonly byte[] OtherPicture = [4, 5, 6];

        private readonly FakeThumbnails _thumbnails = new FakeThumbnails();
        private readonly TestDatabase _database;

        public TemplateThumbnailInterceptorTests()
        {
            _database = new TestDatabase(null, new TemplateThumbnailInterceptor(_thumbnails));
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private async Task<int> InsertAsync()
        {
            await using AppDbContext db = _database.Open();
            FlowTemplate template = new FlowTemplate
            {
                Name = "template-a1b2c.png",
                TemplateImage = Picture,
                Flow = new Flow { Name = "Login" },
            };

            db.FlowTemplates.Add(template);
            await db.SaveChangesAsync(Ct);
            return template.Id;
        }

        private async Task MoveClickPointAsync(int id)
        {
            await using AppDbContext db = _database.Open();
            FlowTemplate template = await db.FlowTemplates.SingleAsync(x => x.Id == id, Ct);
            template.ClickOffsetX = 7;
            await db.SaveChangesAsync(Ct);
        }

        private async Task<byte[]?> ThumbnailAsync(int id)
        {
            await using AppDbContext db = _database.Open();
            FlowTemplate template = await db.FlowTemplates.SingleAsync(x => x.Id == id, Ct);
            return template.Thumbnail;
        }

        [Fact]
        public async Task A_new_picture_is_saved_with_its_thumbnail()
        {
            int id = await InsertAsync();

            byte[]? thumbnail = await ThumbnailAsync(id);
            byte[] expected = FakeThumbnails.Of(Picture);
            thumbnail.ShouldBe(expected);
        }

        [Fact]
        public async Task A_changed_picture_gets_a_new_thumbnail()
        {
            int id = await InsertAsync();

            await using (AppDbContext db = _database.Open())
            {
                FlowTemplate template = await db.FlowTemplates.SingleAsync(x => x.Id == id, Ct);
                template.TemplateImage = OtherPicture;
                await db.SaveChangesAsync(Ct);
            }

            byte[]? thumbnail = await ThumbnailAsync(id);
            byte[] expected = FakeThumbnails.Of(OtherPicture);
            thumbnail.ShouldBe(expected);
        }

        [Fact]
        public async Task Moving_the_click_point_alone_keeps_the_thumbnail()
        {
            int id = await InsertAsync();
            _thumbnails.Calls = 0;

            await MoveClickPointAsync(id);

            _thumbnails.Calls.ShouldBe(0);
            byte[]? thumbnail = await ThumbnailAsync(id);
            byte[] expected = FakeThumbnails.Of(Picture);
            thumbnail.ShouldBe(expected);
        }

        // As the rows written before thumbnails were made: a save that loads the template fills it
        // in, even when nothing about the template changed.
        [Fact]
        public async Task A_picture_saved_without_a_thumbnail_gets_one_when_it_is_saved_again()
        {
            int id = await InsertAsync();
            await using (AppDbContext db = _database.Open())
            {
                await db.FlowTemplates
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(x => x.SetProperty(t => t.Thumbnail, (byte[]?)null), Ct);
            }

            await using (AppDbContext db = _database.Open())
            {
                await db.FlowTemplates.SingleAsync(x => x.Id == id, Ct);
                await db.SaveChangesAsync(Ct);
            }

            byte[]? thumbnail = await ThumbnailAsync(id);
            byte[] expected = FakeThumbnails.Of(Picture);
            thumbnail.ShouldBe(expected);
        }


        // ================================================================
        // Private types
        // ================================================================

        // A thumbnail is the picture with a 9 on the end, so a test can tell which picture it came from.
        private sealed class FakeThumbnails : IOpenCvService
        {
            public int Calls { get; set; }

            public static byte[] Of(byte[] png)
            {
                return [.. png, 9];
            }

            public byte[]? Thumbnail(byte[] png, int maxEdge)
            {
                Calls++;
                return Of(png);
            }

            public TemplateMatchOutcome Match(TemplateMatchRequest request)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<IReadOnlyList<int>> GroupSameElements(IReadOnlyList<ElementPicture> elements)
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
}
