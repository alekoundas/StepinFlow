using System.Drawing;

using Business.Searching;
using Business.AreaPoint;
using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Runs the step's search against the live screen without clicking anything. Takes the whole
    /// dto rather than an id so it works on unsaved form state.
    /// </summary>
    public class TestImageSearchHandler
    {
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IScreenshotService _screenshotService;
        private readonly IImageSearcher _imageSearcher;

        public TestImageSearchHandler(
            IAreaPointResolver areaPointResolver,
            IScreenshotService screenshotService,
            IImageSearcher imageSearcher)
        {
            _areaPointResolver = areaPointResolver;
            _screenshotService = screenshotService;
            _imageSearcher = imageSearcher;
        }

        public async Task<ResultDto<ImageSearchTestResultDto>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            FlowStepDto step = dto;

            if (step.FlowAreaId == null)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed("Pick a search area first."));

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed(area.Error!));

            List<FlowStepTemplateDto> images = step.FlowStepTemplates.ToList();
            List<SearchTemplate> templates = images.Select(x => SearchTemplate.From(x)).ToList();

            // Never stop early: a report that skipped the templates after the first hit would say
            // they were not there.
            ImageSearchResult search = _imageSearcher.Search(area, SearchSettings.From(step), templates, stopAtFirstHit: false);
            if (search.Error != null)
                return ResultDto<ImageSearchTestResultDto>.Success(Failed(search.Error));

            ImageSearchTestResultDto result = new ImageSearchTestResultDto
            {
                IsResolved = true,
                SearchAreaX = area.Bounds.X,
                SearchAreaY = area.Bounds.Y,
                SearchAreaWidth = area.Bounds.Width,
                SearchAreaHeight = area.Bounds.Height,

                // The haystack the search matched against, not a second capture: anything that
                // moved in between would put the boxes over the wrong pixels, and the picture
                // would be believed.
                Screenshot = _screenshotService.Encode(search.Haystack, ScreenshotFormatEnum.JPEG, 80),
            };

            for (int index = 0; index < images.Count; index++)
            {
                FlowStepTemplateDto image = images[index];
                SearchTemplate template = templates[index];
                TemplateMatchOutcome outcome = search.Outcomes[index];

                ImageSearchTestImageDto imageResult = new ImageSearchTestImageDto
                {
                    FlowStepTemplateId = image.Id,
                    Name = image.Name,
                    IsRequired = image.IsRequired,
                };

                IReadOnlyList<TemplateMatchResult> matches = outcome.Matches;

                imageResult.MatchCount = matches.Count;
                imageResult.IsFound = matches.Count > 0;

                // The best thing in the screenshot, hit or not. Without it "0.79 against your 0.80" and
                // "not on screen at all" both read as no result.
                imageResult.BestScore = outcome.BestScore ?? 0f;

                ImageSearchTestMatchDto ToDto(TemplateMatchResult match, bool isAccepted)
                {
                    Point click = template.ClickPoint(match);

                    return new ImageSearchTestMatchDto
                    {
                        IsAccepted = isAccepted,
                        X = match.X,
                        Y = match.Y,
                        Width = match.Width,
                        Height = match.Height,
                        Score = match.Score,
                        Scale = match.Scale,
                        ClickX = click.X,
                        ClickY = click.Y,
                    };
                }

                // Hits first, then the next ones down, all in area relative coordinates so the
                // details view can draw them on the screenshot as they are.
                imageResult.Matches = matches.Select(x => ToDto(x, true))
                    .Concat(outcome.Rejected.Select(x => ToDto(x, false)))
                    .ToList();

                if (matches.Count > 0)
                {
                    TemplateMatchResult best = matches[0];
                    Point bestClick = template.ClickPoint(best);

                    imageResult.BestScore = best.Score;
                    imageResult.Scale = best.Scale;
                    // Absolute, click offset applied and scaled the same as the template.
                    imageResult.BestX = area.Bounds.X + bestClick.X;
                    imageResult.BestY = area.Bounds.Y + bestClick.Y;
                }

                result.Images.Add(imageResult);
                result.TotalMatches += matches.Count;
            }

            // The searcher's verdict, the same one an execution gets.
            result.WouldSucceed = search.Hits.Count > 0;

            return ResultDto<ImageSearchTestResultDto>.Success(result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static ImageSearchTestResultDto Failed(string error)
        {
            return new ImageSearchTestResultDto
            {
                IsResolved = false,
                ErrorMessage = error,
            };
        }
    }
}
