using System.Text.RegularExpressions;

using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class TestWindowMatchHandler : IRequestHandler<TestWindowMatchQuery, ResultDto<WindowMatchTestResultDto>>
    {
        public Task<ResultDto<WindowMatchTestResultDto>> Handle(TestWindowMatchQuery request, CancellationToken ct)
        {
            WindowMatchTestRequestDto dto = request.dto;

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
                try
                {
                    _ = Regex.Match(string.Empty, dto.TitlePattern);
                }
                catch (ArgumentException ex)
                {
                    return Task.FromResult(ResultDto<WindowMatchTestResultDto>.Failure($"That title pattern is not a valid regex: {ex.Message}"));
                }
            }

            List<WindowMatchDto> matches = AppWindowHelper.FindWindowMatches(query)
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
