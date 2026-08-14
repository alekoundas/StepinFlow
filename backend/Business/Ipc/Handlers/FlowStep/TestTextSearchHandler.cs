using Business.Services.FrameService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Reads the step's area off the live screen. Takes the whole dto rather than an id so it
    /// works on unsaved form state, same as the image search test.
    /// </summary>
    public class TestTextSearchHandler : IRequestHandler<TestTextSearchQuery, ResultDto<TextSearchTestResultDto>>
    {
        private readonly IFrameResolver _frameResolver;
        private readonly IScreenshotService _screenshotService;
        private readonly IOcrService _ocrService;

        public TestTextSearchHandler(
            IFrameResolver frameResolver,
            IScreenshotService screenshotService,
            IOcrService ocrService)
        {
            _frameResolver = frameResolver;
            _screenshotService = screenshotService;
            _ocrService = ocrService;
        }

        public async Task<ResultDto<TextSearchTestResultDto>> Handle(TestTextSearchQuery request, CancellationToken ct)
        {
            FlowStepDto step = request.dto;

            if (step.FlowAreaId == null)
                return ResultDto<TextSearchTestResultDto>.Success(new TextSearchTestResultDto { IsResolved = false, ErrorMessage = "Pick a search area first." });

            AreaResolution area = await _frameResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ResultDto<TextSearchTestResultDto>.Success(new TextSearchTestResultDto { IsResolved = false, ErrorMessage = area.Error });

            RawImage image = _screenshotService.CaptureRaw(area.Bounds);
            if (image.IsEmpty)
                return ResultDto<TextSearchTestResultDto>.Success(new TextSearchTestResultDto { IsResolved = false, ErrorMessage = "The search area produced no pixels." });

            string text;
            try
            {
                text = await _ocrService.ReadAsync(image, step.OcrLanguage, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<TextSearchTestResultDto>.Success(new TextSearchTestResultDto { IsResolved = false, ErrorMessage = ex.Message });
            }

            TextSearchTestResultDto result = new TextSearchTestResultDto
            {
                IsResolved = true,
                Text = text,
                IsMatch = Matches(text, step.ConditionText, step.ConditionType),
            };

            result.ResultValue = Extract(text, step.ResultExtractPattern);

            return ResultDto<TextSearchTestResultDto>.Success(result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool Matches(string text, string expected, ConditionTypeEnum? condition) => condition switch
        {
            ConditionTypeEnum.EQUALS => text.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase),
            ConditionTypeEnum.MATCHES_REGEX => Regex.IsMatch(text, expected),
            _ => text.Contains(expected, StringComparison.OrdinalIgnoreCase),
        };

        private static string Extract(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return text;

            Match match = Regex.Match(text, pattern);
            if (!match.Success)
                return string.Empty;

            return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
        }
    }
}
