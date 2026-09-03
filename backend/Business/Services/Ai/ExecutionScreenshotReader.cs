using Business.Services.Ai.AiModels;
using Business.Services.Ai.Providers;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OpenCvSharp;

namespace Business.Services.Ai
{
    /// <summary>
    /// The pictures from an execution that are worth showing a model.
    ///
    /// Three kinds, and the comparison between them is the whole point: the template images the
    /// failing step was hunting for, the screen just before it, and the screen at it. The two
    /// screenshots alone say whether anything changed; the template image says whether the thing was
    /// ever there to find. A score of 0.38 tells you it is not a threshold problem - only the
    /// template image beside the screen says why.
    /// </summary>
    public class ExecutionScreenshotReader : IExecutionScreenshotReader
    {
        // What a vision model is given, not what was captured. OpenClaw settled on the same number
        // for the same reason: past this the extra pixels cost tokens and time without being read.
        private const int _maxEdge = 1200;
        private const int _quality = 70;

        // Four pictures is already a lot to hand a small model on a cpu, and the fifth adds less
        // than it costs.
        private const int _maxTemplateImages = 2;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IAiProviderService _providerService;
        private readonly IAiModelService _modelService;
        private readonly ILogger<ExecutionScreenshotReader> _logger;

        public ExecutionScreenshotReader(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAiProviderService providerService,
            IAiModelService modelService,
            ILogger<ExecutionScreenshotReader> logger)
        {
            _dbContextFactory = dbContextFactory;
            _providerService = providerService;
            _modelService = modelService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<IReadOnlyList<AiImage>> GetForExecutionAsync(int executionId, CancellationToken ct = default)
        {
            if (!await IsAllowedAsync(ct))
                return [];

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ExecutionFolder? folder = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.Id == executionId)
                .Select(x => new ExecutionFolder(x.ScreenshotFolderName, x.ErrorFlowStepId))
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(folder?.FolderName))
                return [];

            List<StepScreenshot> screenshots = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == executionId && x.ScreenshotFileName != null)
                .OrderBy(x => x.Sequence)
                .Select(x => new StepScreenshot(x.Sequence, x.FlowStepId, x.Outcome, x.ScreenshotFileName!))
                .ToListAsync(ct);

            if (screenshots.Count == 0)
                return [];

            // The step that ended it, or failing that the last one that failed at all. An execution
            // that went fine still kept screenshots worth looking at, so the last one stands in.
            int failedAt = screenshots
                .Where(x => folder.ErrorFlowStepId != null && x.FlowStepId == folder.ErrorFlowStepId)
                .Select(x => x.Sequence)
                .DefaultIfEmpty(screenshots.LastOrDefault(x => x.Outcome == StepOutcomeEnum.FAILURE)?.Sequence ?? screenshots[^1].Sequence)
                .First();

            int index = screenshots.FindIndex(x => x.Sequence == failedAt);
            if (index < 0)
                index = screenshots.Count - 1;

            List<AiImage> images = new List<AiImage>();

            // What it was looking for comes first: everything after it is read against it.
            images.AddRange(await GetTemplateImagesAsync(dbContext, screenshots[index].FlowStepId, ct));

            // Then the screenshots, oldest first, so "before" and "after" read in the order they
            // happened.
            if (index > 0)
                AddScreenshot(images, folder.FolderName, screenshots[index - 1].ScreenshotFileName, "The screen just BEFORE the step that failed.");

            AddScreenshot(images, folder.FolderName, screenshots[index].ScreenshotFileName, "The screen AT the step that failed. This is what the search actually looked at.");

            return images;
        }

        public async Task<bool> IsAllowedAsync(CancellationToken ct = default)
        {
            if (!await _modelService.SupportsVisionAsync(ct))
                return false;

            // The same rule the screen text follows: local is always fine, cloud only if asked for.
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);
            if (provider == AiProviderEnum.OLLAMA)
                return true;

            return await _providerService.IsScreenContentAllowedAsync(ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The template images the failing step was hunting for. Required ones first: those are the ones that had to be found, so they are the ones worth the model's attention.
        private async Task<IReadOnlyList<AiImage>> GetTemplateImagesAsync(AppDbContext dbContext, int? flowStepId, CancellationToken ct)
        {
            if (flowStepId == null)
                return [];

            List<StepTemplateImage> templateImages = await dbContext.FlowStepImages
                .AsNoTracking()
                .Where(x => x.FlowStepId == flowStepId && x.TemplateImage != null)
                .OrderByDescending(x => x.IsRequired)
                .ThenBy(x => x.OrderNumber)
                .Take(_maxTemplateImages)
                .Select(x => new StepTemplateImage(x.Name, x.TemplateImage!))
                .ToListAsync(ct);

            List<AiImage> images = new List<AiImage>();

            foreach (StepTemplateImage templateImage in templateImages)
            {
                byte[]? flattened = Flatten(templateImage.TemplateImage);
                if (flattened == null)
                    continue;

                images.Add(new AiImage
                {
                    Label = $"The template image it was searching for, \"{templateImage.Name}\". Anything erased from it shows as white and was not part of the search.",
                    Bytes = flattened,
                    MediaType = "image/png",
                });
            }

            return images;
        }

        private void AddScreenshot(List<AiImage> images, string folderName, string fileName, string label)
        {
            byte[]? bytes = Read(folderName, fileName);
            if (bytes == null)
                return;

            images.Add(new AiImage { Label = label, Bytes = bytes });
        }

        // Template images are png with erased pixels. 
        // The erased pixels must set as white and explain what white means in prompt.
        // Stays png rather than jpeg: a template image is all hard edges, and jpeg rings around them.
        private static byte[]? Flatten(byte[] png)
        {
            try
            {
                using Mat decoded = Cv2.ImDecode(png, ImreadModes.Unchanged);
                if (decoded.Empty())
                    return null;

                if (decoded.Channels() != 4)
                    return decoded.ImEncode(".png");

                using Mat colour = new Mat();
                Cv2.CvtColor(decoded, colour, ColorConversionCodes.BGRA2BGR);

                // Only the fully erased pixels. A threshold rather than an inverted alpha, because
                // inverting turns a half transparent pixel fully white, and those were part of the
                // search - at less weight, but they were there.
                using Mat alpha = decoded.ExtractChannel(3);
                using Mat erased = new Mat();
                Cv2.Threshold(alpha, erased, 0, 255, ThresholdTypes.BinaryInv);

                colour.SetTo(Scalar.All(255), erased);

                return colour.ImEncode(".png");
            }
            catch (Exception)
            {
                return null;
            }
        }

        // A missing file is not an error. 
        private byte[]? Read(string folderName, string fileName)
        {
            try
            {
                string path = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), folderName, fileName);
                if (!File.Exists(path))
                    return null;

                return Downscale(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read the screenshot {FileName}.", fileName);

                return null;
            }
        }

        // Sent at the size the model reads it at rather than the size the screen was.
        private static byte[] Downscale(byte[] jpeg)
        {
            using Mat decoded = Cv2.ImDecode(jpeg, ImreadModes.Color);
            if (decoded.Empty())
                return jpeg;

            int longest = Math.Max(decoded.Width, decoded.Height);
            if (longest <= _maxEdge)
                return jpeg;

            double scale = (double)_maxEdge / longest;

            using Mat resized = new Mat();
            Cv2.Resize(decoded, resized, new Size(0, 0), scale, scale, InterpolationFlags.Area);

            return resized.ImEncode(".jpg", [(int)ImwriteFlags.JpegQuality, _quality]);
        }

        // ================================================================
        // Private types
        // ================================================================

        private sealed record ExecutionFolder(string? FolderName, int? ErrorFlowStepId);
        private sealed record StepScreenshot(int Sequence, int? FlowStepId, StepOutcomeEnum Outcome, string ScreenshotFileName);
        private sealed record StepTemplateImage(string Name, byte[] TemplateImage);
    }
}
