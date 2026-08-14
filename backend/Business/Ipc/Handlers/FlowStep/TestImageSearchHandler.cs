using Business.Services.AreaPointService;
using Business.Services.MatchService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Runs the step's search against the live screen without clicking anything. Takes the whole
    /// dto rather than an id so it works on unsaved form state.
    /// </summary>
    public class TestImageSearchHandler : IRequestHandler<TestImageSearchQuery, ResultDto<ImageSearchTestResultDto>>
    {
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IScreenshotService _screenshotService;
        private readonly IOpenCvService _templateMatcher;

        public TestImageSearchHandler(
            IAreaPointResolver areaPointResolver,
            IScreenshotService screenshotService,
            IOpenCvService templateMatcher)
        {
            _areaPointResolver = areaPointResolver;
            _screenshotService = screenshotService;
            _templateMatcher = templateMatcher;
        }

        public async Task<ResultDto<ImageSearchTestResultDto>> Handle(TestImageSearchQuery request, CancellationToken ct)
        {
            FlowStepDto step = request.dto;

            if (step.FlowAreaId == null)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed("Pick a search area first."));

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed(area.Error!));

            RawImage haystack = _screenshotService.CaptureRaw(area.Bounds);
            if (haystack.IsEmpty)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed("The search area produced no pixels."));

            ImageSearchTestResultDto result = new ImageSearchTestResultDto
            {
                IsResolved = true,
                SearchAreaX = area.Bounds.X,
                SearchAreaY = area.Bounds.Y,
                SearchAreaWidth = area.Bounds.Width,
                SearchAreaHeight = area.Bounds.Height,
            };

            foreach (FlowStepImageDto image in step.FlowStepImages)
            {
                ImageSearchTestImageDto imageResult = new ImageSearchTestImageDto
                {
                    FlowStepImageId = image.Id,
                    Name = image.Name,
                    IsRequired = image.IsRequired,
                };

                IReadOnlyList<TemplateMatch> matches = _templateMatcher.Match(new TemplateMatchRequest
                {
                    Haystack = haystack,
                    TemplateImage = image.TemplateImage ?? [],
                    Mode = image.TemplateMatchMode ?? step.TemplateMatchMode,
                    Threshold = image.Accuracy ?? step.Accuracy,
                    ScaleRatio = ScaleRatio(image, area.Bounds.Width),
                    AllowMultiScale = image.AllowMultiScale,
                    ScaleTolerance = image.ScaleTolerance,
                    MaxMatches = step.MaxMatches,
                });

                imageResult.MatchCount = matches.Count;
                imageResult.IsFound = matches.Count > 0;

                if (matches.Count > 0)
                {
                    TemplateMatch best = matches[0];

                    imageResult.BestScore = best.Score;
                    imageResult.Scale = best.Scale;
                    // Absolute, click offset applied and scaled the same as the template.
                    imageResult.BestX = area.Bounds.X + best.X + (int)MathF.Round(image.ClickOffsetX * best.Scale);
                    imageResult.BestY = area.Bounds.Y + best.Y + (int)MathF.Round(image.ClickOffsetY * best.Scale);
                }

                result.Images.Add(imageResult);
                result.TotalMatches += matches.Count;
            }

            result.WouldSucceed = WouldSucceed(result);

            return ResultDto<ImageSearchTestResultDto>.Success(result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // No image marked required means any one of them is enough, which is the "three variants
        // of the same icon" case. Mark some and all of those have to be there.
        private static bool WouldSucceed(ImageSearchTestResultDto result)
        {
            List<ImageSearchTestImageDto> required = result.Images.Where(x => x.IsRequired).ToList();

            return required.Count > 0
                ? required.All(x => x.IsFound)
                : result.Images.Any(x => x.IsFound);
        }

        private static float ScaleRatio(FlowStepImageDto image, int currentFrameWidth)
        {
            if (image.AuthoredFrameWidth <= 0 || currentFrameWidth <= 0)
                return 1f;

            return (float)currentFrameWidth / image.AuthoredFrameWidth;
        }

        private static ImageSearchTestResultDto Failed(string error) => new ImageSearchTestResultDto
        {
            IsResolved = false,
            ErrorMessage = error,
        };
    }
}
