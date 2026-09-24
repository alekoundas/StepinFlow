using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;

namespace Business.Flows
{
    /// <summary>
    /// Templates are edited as part of their step, so they are matched by Id and updated in place rather than replaced. 
    /// </summary>
    public static class FlowStepTemplateSync
    {
        public static void Sync(AppDbContext dbContext, FlowStep step, IEnumerable<FlowStepTemplateDto> dtos)
        {
            List<FlowStepTemplate> existing = step.FlowStepTemplates.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowStepTemplate removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowStepTemplates.Remove(removed);

            int order = 0;
            foreach (FlowStepTemplateDto dto in dtos)
            {
                FlowStepTemplate? image = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (image == null)
                {
                    image = new FlowStepTemplate { FlowStep = step };
                    dbContext.FlowStepTemplates.Add(image);
                }

                image.Name = dto.Name;
                image.OrderNumber = order++;
                image.IsRequired = dto.IsRequired;

                // Only overwrite the blob when the client actually sent one: the list view sends
                // templates back without their bytes so a save does not push megabytes per step.
                if (dto.TemplateImage != null && dto.TemplateImage.Length > 0)
                {
                    image.TemplateImage = dto.TemplateImage;
                    //image.Thumbnail = ThumbnailHelper.Create(dto.TemplateImage);
                }

                image.Accuracy = dto.Accuracy;

                image.ClickOffsetX = dto.ClickOffsetX;
                image.ClickOffsetY = dto.ClickOffsetY;

                image.AuthoredFlowAreaWidth = dto.AuthoredFlowAreaWidth;
                image.AuthoredFlowAreaHeight = dto.AuthoredFlowAreaHeight;
                image.AuthoredDpi = dto.AuthoredDpi;
            }
        }
    }
}
