using Core.Models.Database;
using Core.Ports;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DataAccess.Interceptors
{
    /// <summary>
    /// Makes a template's thumbnail on the way to the database, so whichever path saves a picture -
    /// a capture, an edit, an import, a recording - the tree shows the picture that is saved.
    /// </summary>
    public sealed class TemplateThumbnailInterceptor : SaveChangesInterceptor
    {
        // Sharp at twice the size a tree row draws it.
        private const int THUMBNAIL_EDGE = 96;

        private readonly IOpenCvService _openCvService;

        public TemplateThumbnailInterceptor(IOpenCvService openCvService)
        {
            _openCvService = openCvService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Refresh(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Refresh(eventData.Context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A new picture, a changed one, or any loaded one saved before thumbnails were made, even
        // when nothing else about it changed.
        private void Refresh(DbContext? dbContext)
        {
            if (dbContext == null)
                return;

            foreach (EntityEntry<FlowTemplate> entry in dbContext.ChangeTracker.Entries<FlowTemplate>())
            {
                if (entry.State == EntityState.Deleted || entry.State == EntityState.Detached)
                    continue;

                bool isNewPicture = entry.State == EntityState.Added || entry.Property(x => x.TemplateImage).IsModified;
                if (!isNewPicture && (entry.Entity.Thumbnail != null || entry.Entity.TemplateImage == null))
                    continue;

                byte[]? picture = entry.Entity.TemplateImage;
                byte[]? thumbnail = null;
                if (picture != null)
                    thumbnail = _openCvService.Thumbnail(picture, THUMBNAIL_EDGE);

                entry.Property(x => x.Thumbnail).CurrentValue = thumbnail;
            }
        }
    }
}
