using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;

namespace Business.Helpers
{
    /// <summary>
    /// Templates are edited as part of their step, so they are matched by Id and updated in place rather than replaced. 
    /// </summary>
    public static class FlowStepImageSyncHelper
    {
        public static void Sync(AppDbContext dbContext, FlowStep step, IEnumerable<FlowStepImageDto> dtos)
        {
            List<FlowStepImage> existing = step.FlowStepImages.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowStepImage removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowStepImages.Remove(removed);

            int order = 0;
            foreach (FlowStepImageDto dto in dtos)
            {
                FlowStepImage? image = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (image == null)
                {
                    image = new FlowStepImage { FlowStep = step };
                    dbContext.FlowStepImages.Add(image);
                }

                image.Name = dto.Name;
                image.OrderNumber = order++;
                image.IsRequired = dto.IsRequired;

                // Only overwrite the blob when the client actually sent one: the list view sends
                // templates back without their bytes so a save does not push megabytes per step.
                if (dto.TemplateImage != null && dto.TemplateImage.Length > 0)
                    image.TemplateImage = dto.TemplateImage;

                image.TemplateMatchMode = dto.TemplateMatchMode;
                image.Accuracy = dto.Accuracy;

                image.ClickOffsetX = dto.ClickOffsetX;
                image.ClickOffsetY = dto.ClickOffsetY;

                image.AuthoredFrameWidth = dto.AuthoredFrameWidth;
                image.AuthoredFrameHeight = dto.AuthoredFrameHeight;
                image.AuthoredMonitorId = dto.AuthoredMonitorId;
                image.AuthoredMonitorDpi = dto.AuthoredMonitorDpi;

                image.AllowMultiScale = dto.AllowMultiScale;
                image.ScaleTolerance = dto.ScaleTolerance;
            }
        }
    }
}
