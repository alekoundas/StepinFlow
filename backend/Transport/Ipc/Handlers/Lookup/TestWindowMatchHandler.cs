using Core.Ports;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class TestWindowMatchHandler
    {
        private readonly IWindowService _windowService;

        public TestWindowMatchHandler(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public Task<ResultDto<WindowMatchTestResultDto>> HandleAsync(WindowMatchTestRequestDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.ProcessName) && string.IsNullOrWhiteSpace(dto.TitlePattern))
                return Task.FromResult(ResultDto<WindowMatchTestResultDto>.Failure("Pick an application or type a title first, or this matches whatever window is in front."));

            WindowQuery query = new WindowQuery
            {
                ProcessName = dto.ProcessName,
                TitlePattern = dto.TitlePattern,
                TitleMatchMode = dto.TitleMatchMode,
                UseClientArea = dto.UseClientArea,
            };

            if (dto.TitleMatchMode == TitleMatchModeEnum.REGEX && !string.IsNullOrEmpty(dto.TitlePattern))
            {
                string patternError = RegexHelper.PatternError(dto.TitlePattern);
                if (patternError.Length > 0)
                    return Task.FromResult(ResultDto<WindowMatchTestResultDto>.Failure($"That title pattern is not a valid regex: {patternError}"));
            }

            List<WindowMatchDto> matches = _windowService.FindWindowMatches(query)
                .Select(x => new WindowMatchDto
                {
                    Title = x.Title,
                    ProcessName = x.ProcessName,
                    X = x.Bounds.X,
                    Y = x.Bounds.Y,
                    Width = x.Bounds.Width,
                    Height = x.Bounds.Height,
                })
                .ToList();

            return Task.FromResult(ResultDto<WindowMatchTestResultDto>.Success(new WindowMatchTestResultDto { Matches = matches }));
        }
    }
}
