using Business.Services.AreaPointService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Reads the step's area off the live screen. Takes the whole dto rather than an id so it
    /// works on unsaved form state, same as the image search test.
    /// </summary>
    public class TestSearchTextHandler : IRequestHandler<TestSearchTextQuery, ResultDto<SearchTextTestResultDto>>
    {
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IScreenshotService _screenshotService;
        private readonly IOcrService _ocrService;

        public TestSearchTextHandler(
            IAreaPointResolver areaPointResolver,
            IScreenshotService screenshotService,
            IOcrService ocrService)
        {
            _areaPointResolver = areaPointResolver;
            _screenshotService = screenshotService;
            _ocrService = ocrService;
        }

        public async Task<ResultDto<SearchTextTestResultDto>> Handle(TestSearchTextQuery request, CancellationToken ct)
        {
            FlowStepDto step = request.dto;

            if (step.FlowAreaId == null)
                return Unresolved("Pick an area to read first.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return Unresolved(area.Error);

            RawImage image = _screenshotService.CaptureRaw(area.Bounds);
            if (image.IsEmpty)
                return Unresolved("The area produced no pixels.");

            string text;
            try
            {
                text = await _ocrService.ReadAsync(image, step.OcrLanguage, ct);
            }
            catch (Exception ex)
            {
                return Unresolved(ex.Message);
            }

            // Narrowed first, then tested, so the two fields compose: keep only the part that
            // matters, then say what has to be true of it. Same order the worker uses, so what the
            // form previews is what the execution will do.
            string value = TextExtractHelper.Extract(text, step.ResultExtractPattern);

            return ResultDto<SearchTextTestResultDto>.Success(new SearchTextTestResultDto
            {
                IsResolved = true,
                Text = text,
                ResultValue = value,
                IsMatch = ConditionEvaluator.IsSatisfied(value, step.ConditionType, step.ConditionText, step.ConditionTextEnd),
            });
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static ResultDto<SearchTextTestResultDto> Unresolved(string? error)
        {
            return ResultDto<SearchTextTestResultDto>.Success(new SearchTextTestResultDto
            {
                IsResolved = false,
                ErrorMessage = error,
            });
        }
    }
}
