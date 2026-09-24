using Core.Catalogs;
using System.ComponentModel;
using Business.Ai.Helpers;
using Business.Validation;
using Core.Enums;
using Core.Models.Business;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Ai.Tools
{
    /// <summary>
    /// What the model may ask the database.
    ///
    /// Read only, and shaped for reading rather than for the app: a step comes back as the handful
    /// of fields that mean something for the question being asked, not as the fifty five column dto
    /// the forms use. A whole flow at full width would fill the context on the first call.
    ///
    /// Two columns are never selected. The api key and a bot's webhook url are credentials, and the
    /// answer to "what is my api key" should be that nothing here can read it.
    ///
    /// A third is conditional. What a flow was recorded typing is whatever was on screen at the
    /// time - a password into a login form as readily as a search term - so it is redacted unless
    /// this provider may be shown screen data, and not searched either.
    /// </summary>
    public sealed class DbQueryTools
    {
        private const int _maxRows = 50;
        private const string _redacted = "(hidden)";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidationService _flowValidationService;
        private readonly bool _canSendScreenData;

        public DbQueryTools(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService, bool canSendScreenData)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
            _canSendScreenData = canSendScreenData;
        }


        // ================================================================
        // Public methods
        // ================================================================

        [Description("Lists flows whose name or description matches the text. Pass an empty string to list every flow. Use this first when the question names a flow.")]
        public async Task<IReadOnlyList<FlowSummary>> SearchFlows([Description("Text to match in the name or description. Empty lists all.")] string text)
        {
            List<string> tokens = SearchPatterns(text);

            // Nothing worth searching for - whitespace included - means the whole list, which is
            // what the description promises for an empty string.
            bool isListAll = tokens.Count == 0;

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Flows
                .AsNoTracking()
                .Where(x => isListAll || tokens.Any(t => EF.Functions.Like(x.Name, t) || EF.Functions.Like(x.Description, t)))
                .OrderBy(x => x.Name)
                .Take(_maxRows)
                .Select(x => new FlowSummary(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.IsSubFlow,
                    x.FlowSteps.Count(),
                    x.FlowAreas.Count(),
                    x.FlowPoints.Count()))
                .ToListAsync();
        }

        [Description("One flow with the areas and points it defines. Areas say which application or monitor the flow works against and what their contents scale with; points say what they are measured from. An area or point with nothing around it is in screen coordinates, and will not survive another screen.")]
        public async Task<FlowDetail?> GetFlow([Description("The flow id, from SearchFlows.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            FlowSummary? flow = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == flowId)
                .Select(x => new FlowSummary(x.Id, x.Name, x.Description, x.IsSubFlow, x.FlowSteps.Count(), 0, 0))
                .FirstOrDefaultAsync();

            if (flow == null)
                return null;

            List<Core.Models.Database.FlowArea> areas = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowArea)
                .Where(x => x.FlowId == flowId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            List<Core.Models.Database.FlowPoint> points = await dbContext.FlowPoints
                .AsNoTracking()
                .Include(x => x.FlowArea)
                .Where(x => x.FlowId == flowId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new FlowDetail(
                flow.Id,
                flow.Name,
                flow.Description,
                flow.IsSubFlow,
                flow.StepCount,
                areas.Select(AreaSummaryOf).ToList(),
                points.Select(PointSummaryOf).ToList());
        }

        [Description("The steps of one flow, in tree order. Optionally filtered to one step type.")]
        public async Task<IReadOnlyList<StepSummary>> GetFlowSteps(
            [Description("The flow id.")] int flowId,
            [Description("Optional step type, for example SEARCH_IMAGE, SEARCH_TEXT, CURSOR_CLICK, KEYBOARD_INPUT, SYSTEM_COMMAND. Empty returns every step.")] string flowStepType)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            List<StepSummary> steps = await Steps(dbContext)
                .Where(x => x.FlowId == flowId || x.RootId == flowId)
                .Where(x => flowStepType == "" || x.FlowStepType.ToString() == flowStepType)
                .OrderBy(x => x.ParentFlowStepId)
                .ThenBy(x => x.OrderNumber)
                .Take(_maxRows * 4)
                .Select(Projection())
                .ToListAsync();

            return Redact(steps);
        }

        [Description("Searches every flow's steps for text, across process names, window titles, typed text, commands, conditions and step names. Use this for questions like 'which flows use Chrome'.")]
        public async Task<IReadOnlyList<StepSummary>> SearchSteps(
            [Description("Text to look for, for example an application name.")] string text,
            [Description("Optional step type to narrow to. Empty searches every type.")] string flowStepType)
        {
            List<string> tokens = SearchPatterns(text);
            if (tokens.Count == 0)
                return [];

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Typed text is not searched either when it may not be shown: a search that answers
            // "does any flow type this" confirms a guess just as well as reading it would.
            bool canSearchTypedText = _canSendScreenData;

            List<StepSummary> steps = await Steps(dbContext)
                .Where(x => flowStepType == "" || x.FlowStepType.ToString() == flowStepType)
                .Where(x => tokens.Any(t =>
                    EF.Functions.Like(x.Name, t) ||
                    EF.Functions.Like(x.ProcessName, t) ||
                    EF.Functions.Like(x.TitlePattern, t) ||
                    (canSearchTypedText && EF.Functions.Like(x.KeyboardInputText, t)) ||
                    EF.Functions.Like(x.RunCommandValue, t) ||
                    EF.Functions.Like(x.ConditionText, t)))
                .Take(_maxRows)
                .Select(Projection())
                .ToListAsync();

            return Redact(steps);
        }

        [Description("Everything one step is configured with - only the settings its type actually uses, plus its area and templates where it has them. Use this before suggesting why a step misbehaves.")]
        public async Task<StepDetail?> GetFlowStepDetail([Description("The step id, from GetFlowSteps or SearchSteps.")] int flowStepId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            Core.Models.Database.FlowStep? step = await dbContext.FlowSteps
                .AsNoTracking()
                .Include(x => x.FlowStepTemplates)
                .Include(x => x.FlowArea)
                .ThenInclude(x => x!.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == flowStepId);

            if (step == null)
                return null;

            // Get columns with value via Reflection.
            Dictionary<string, object?> settings = new Dictionary<string, object?>();
            foreach (string field in FlowStepFieldCatalog.FieldsFor(step.FlowStepType))
            {
                if (!_canSendScreenData && field == nameof(Core.Models.Database.FlowStep.KeyboardInputText))
                {
                    settings[field] = _redacted;
                    continue;
                }

                object? value = typeof(Core.Models.Database.FlowStep).GetProperty(field)?.GetValue(step);
                settings[field] = value is Enum ? value.ToString() : value;
            }

            AreaSummary? area = null;
            if (step.FlowArea != null)
                area = AreaSummaryOf(step.FlowArea);

            List<TemplateSummary> templates = step.FlowStepTemplates
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TemplateSummary(
                    x.Id,
                    x.Name,
                    x.IsRequired,
                    x.Accuracy,
                    x.ClickOffsetX,
                    x.ClickOffsetY,
                    x.AuthoredFlowAreaWidth,
                    x.AuthoredFlowAreaHeight,
                    x.AuthoredDpi))
                .ToList();

            return new StepDetail(
                step.Id,
                step.RootId,
                step.Name,
                step.FlowStepType.ToString(),
                settings,
                area,
                templates);
        }

        [Description("Recent runs, newest first. Says whether each finished, was stopped, or ended with an error.")]
        public async Task<IReadOnlyList<RunSummary>> GetRuns([Description("Optional flow id to narrow to. Zero returns runs of every flow.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Executions
                .AsNoTracking()
                .Where(x => flowId == 0 || x.FlowId == flowId)
                .OrderByDescending(x => x.Id)
                .Take(_maxRows)
                .Select(x => new RunSummary(
                    x.Id,
                    x.FlowId,
                    x.Flow.Name,
                    x.Status.ToString(),
                    x.CreatedOn,
                    x.StepCount,
                    x.ErrorMessage))
                .ToListAsync();
        }

        [Description("The steps of one run, in the order they happened. Indent by depth to read it as a tree. Says of each failure whether it ended the run or was caught by a Failure branch. An image search's BestScore comes with the template that scored it and that template's own accuracy - read one against the other.")]
        public async Task<IReadOnlyList<RunStepSummary>> GetRunSteps([Description("The run id, from GetRuns.")] int executionId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Which failure ended the run is a fact about the run, not about any step, so it has to
            // be read before the steps can say what kind of failure they were.
            int? errorFlowStepId = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.Id == executionId)
                .Select(x => x.ErrorFlowStepId)
                .FirstOrDefaultAsync();

            List<RunStep> steps = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == executionId)
                .OrderBy(x => x.Sequence)
                .Take(_maxRows * 4)
                .Select(x => new RunStep(
                    x.Sequence,
                    x.Depth,
                    x.Name,
                    x.FlowStepType.ToString(),
                    x.Outcome,
                    x.FlowStepId,
                    x.DurationMilliseconds,
                    x.Value,
                    x.Message,
                    x.ExitCode,
                    x.BestScore,
                    x.BestTemplateId))
                .ToListAsync();

            // Each template has its own accuracy, so a score is only readable next to the one that
            // produced it. As the template stands now, the way the execution page reads it.
            List<int> templateIds = steps
                .Where(x => x.BestTemplateId != null)
                .Select(x => x.BestTemplateId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, Core.Models.Database.FlowStepTemplate> closest = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => templateIds.Contains(x.Id))
                .Select(x => new Core.Models.Database.FlowStepTemplate { Id = x.Id, Name = x.Name, Accuracy = x.Accuracy })
                .ToDictionaryAsync(x => x.Id);

            return steps
                .Select(x => new RunStepSummary(
                    x.Sequence,
                    x.Depth,
                    x.Name,
                    x.Type,
                    x.Outcome.ToString(),
                    StepFailureHelper.EndedRun(x.FlowStepId, errorFlowStepId),
                    StepFailureHelper.WasHandled(x.Outcome, x.FlowStepId, errorFlowStepId),
                    x.DurationMilliseconds,
                    x.Value,
                    x.Message,
                    x.ExitCode,
                    x.BestScore,
                    closest.GetValueOrDefault(x.BestTemplateId ?? 0)?.Name,
                    closest.GetValueOrDefault(x.BestTemplateId ?? 0)?.Accuracy))
                .ToList();
        }

        [Description("How many steps of each type a flow has. Use this for \"what does this flow mostly do\" instead of listing every step.")]
        public async Task<IReadOnlyList<StepTypeCount>> CountStepsByType([Description("The flow id.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await Steps(dbContext)
                .Where(x => x.RootId == flowId)
                .GroupBy(x => x.FlowStepType)
                .Select(g => new StepTypeCount(g.Key.ToString(), g.Count()))
                .ToListAsync();
        }

        [Description("What a flow verifies: every check it contains, the section it belongs to, whether failing it ends the execution, and what that failure means. Use this for \"what does this flow test\", and before proposing a fix, because a check nothing acts on is the usual reason a flow passes while the application is broken.")]
        public async Task<IReadOnlyList<FlowCheckSummary>> GetFlowChecks([Description("The flow id, from SearchFlows.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Success and Failure rows are kept here, unlike everywhere else in this class: the walk
            // from a check to the End Execution its failure reaches goes straight through them.
            //
            // Not capped either, unlike everywhere else. A truncated tree gives confident wrong
            // answers - a check whose Failure branch was cut looks like one that ends nothing - so
            // the row limit belongs on the checks that come out, not on the steps going in.
            List<FlowCheckNode> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == flowId)
                .OrderBy(x => x.ParentFlowStepId)
                .ThenBy(x => x.OrderNumber)
                .Select(x => new FlowCheckNode
                {
                    Id = x.Id,
                    ParentFlowStepId = x.ParentFlowStepId,
                    FlowStepType = x.FlowStepType,
                    OrderNumber = x.OrderNumber,
                    Name = x.Name,
                    CodeComment = x.CodeComment,
                    Message = x.Message,
                    EndExecutionAsSuccess = x.EndExecutionAsSuccess,
                })
                .ToListAsync();

            return _flowValidationService.GetChecks(steps)
                .Take(_maxRows)
                .Select(x => new FlowCheckSummary(
                    x.FlowStepId,
                    x.Name,
                    x.CodeComment,
                    x.FlowStepType.ToString(),
                    x.MarkerName,
                    x.IsFatal,
                    x.FailureMessage))
                .ToList();
        }

        [Description("How many runs finished, were stopped, or ended with an error. Use this for \"how reliable is this flow\" instead of listing runs.")]
        public async Task<IReadOnlyList<RunOutcomeCount>> CountRunOutcomes([Description("Optional flow id. Zero counts runs of every flow.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Executions
                .AsNoTracking()
                .Where(x => flowId == 0 || x.FlowId == flowId)
                .GroupBy(x => x.Status)
                .Select(g => new RunOutcomeCount(g.Key.ToString(), g.Count()))
                .ToListAsync();
        }

        [Description("The application settings and their current values, so a problem caused by a setting can be seen. The api key is never included.")]
        public async Task<IReadOnlyList<SettingSummary>> GetSettings()
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            Dictionary<AppSettingKeyEnum, string> stored = await dbContext.AppSettings
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Key, x => x.Value);

            return AppSettingCatalog.All
                .Where(x => x.Kind != AppSettingKindEnum.SECRET)
                .Select(x => new SettingSummary(
                    x.Key.ToString(),
                    x.Label,
                    x.Description,
                    stored.TryGetValue(x.Key, out string? value) ? value : x.DefaultAsText,
                    stored.ContainsKey(x.Key)))
                .ToList();
        }

        [Description("The Discord bots notifications can be sent through. Webhook urls are never included.")]
        public async Task<IReadOnlyList<BotSummary>> GetDiscordBots()
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.DiscordBots
                .AsNoTracking()
                .Take(_maxRows)
                .Select(x => new BotSummary(x.Id, x.Name, x.BotName, x.RateLimitSeconds))
                .ToListAsync();
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// A phrase matched a word at a time rather than whole, because "Google Chrome" has to find
        /// chrome.exe and a single LIKE over the phrase never will. Recall bought with precision:
        /// a step matching only "google" comes back too.
        /// </summary>
        private static List<string> SearchPatterns(string text)
        {
            char[] tokenSeparators = [' ', '\t', ',', ';', '/', '\\', '"', '\''];
            int maxSearchTokens = 4; //Enough for "google chrome browser". More words than that is not a search term.
            return text
                .Split(tokenSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => StripExecutableSuffix(x.Trim().ToLowerInvariant()))
                .Where(x => x.Length >= 2)
                .Distinct()
                .Take(maxSearchTokens)
                .Select(x => $"%{x}%")
                .ToList();
        }

        /// <summary>Typing chrome.exe should find the same rows as typing chrome.</summary>
        private static string StripExecutableSuffix(string token)
        {
            string[] executableSuffixes = [".exe", ".app", ".com"];

            foreach (string suffix in executableSuffixes)
            {
                if (token.Length > suffix.Length && token.EndsWith(suffix, StringComparison.Ordinal))
                    return token[..^suffix.Length];
            }

            return token;
        }

        // What a flow was recorded typing is what was on the screen at the time - a password into a
        // login form as readily as a search term. The recorder cannot tell which, so neither can this.
        private IReadOnlyList<StepSummary> Redact(List<StepSummary> steps)
        {
            if (_canSendScreenData)
                return steps;

            return steps
                .Select(x => x.TypedText.Length == 0 ? x : x with { TypedText = _redacted })
                .ToList();
        }

        private static AreaSummary AreaSummaryOf(Core.Models.Database.FlowArea area)
        {
            string monitor = area.MonitorDeviceName;
            if (area.Type == FlowAreaTypeEnum.MONITOR && monitor.Length == 0)
                monitor = "primary";

            return new AreaSummary(
                area.Id,
                area.Name,
                area.Type.ToString(),
                area.ParentFlowArea?.Name,
                ScalesWithOf(area),
                area.AuthoredDpi,
                area.ProcessName,
                area.TitlePattern,
                monitor,
                area.Width,
                area.Height);
        }

        // Said as what it resolves to: null means "whatever the parent says", which the model
        // should not have to work out.
        private static string ScalesWithOf(Core.Models.Database.FlowArea area)
        {
            if (area.ScalesWith != null)
                return area.ScalesWith.Value.ToString();

            if (area.ParentFlowArea?.ScalesWith != null)
                return $"{area.ParentFlowArea.ScalesWith.Value} (from {area.ParentFlowArea.Name})";

            return "DPI (default)";
        }

        private static PointSummary PointSummaryOf(Core.Models.Database.FlowPoint point)
        {
            return new PointSummary(
                point.Id,
                point.Name,
                point.FlowArea?.Name,
                point.OffsetMode.ToString(),
                point.LocationX,
                point.LocationY,
                point.RatioX,
                point.RatioY,
                point.AuthoredDpi);
        }

        private static IQueryable<Core.Models.Database.FlowStep> Steps(AppDbContext dbContext)
        {
            return dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE);
        }

        /// <summary>
        /// The fields that answer a question about a step. Every type shares one shape, so a step
        /// that does not use a field simply leaves it empty rather than needing its own type.
        /// </summary>
        private static System.Linq.Expressions.Expression<Func<Core.Models.Database.FlowStep, StepSummary>> Projection()
        {
            return x => new StepSummary(
                x.Id,
                x.RootId,
                x.Name,
                x.FlowStepType.ToString(),
                x.ProcessName,
                x.TitlePattern,
                x.KeyboardInputText,
                x.RunCommandValue,
                x.ConditionText,
                x.SubFlowId,
                x.FlowAreaId,
                x.FlowPointId);
        }


        // ================================================================
        // Public types
        // ================================================================

        public record FlowSummary(int Id, string Name, string Description, bool IsSubFlow, int StepCount, int AreaCount, int PointCount);

        public record FlowDetail(int Id, string Name, string Description, bool IsSubFlow, int StepCount, List<AreaSummary> Areas, List<PointSummary> Points);

        /// <summary>
        /// <c>ParentName</c> null and <c>Type</c> CUSTOM is a region in screen coordinates.
        /// <c>AuthoredDpi</c> 0 means its pixels are never scaled.
        /// </summary>
        public record AreaSummary(int Id, string Name, string Type, string? ParentName, string ScalesWith, int AuthoredDpi, string ProcessName, string TitlePattern, string Monitor, int Width, int Height);

        /// <summary><c>AreaName</c> null is a screen coordinate.</summary>
        public record PointSummary(int Id, string Name, string? AreaName, string OffsetMode, int X, int Y, float RatioX, float RatioY, int AuthoredDpi);

        public record StepSummary(int Id, int FlowId, string Name, string Type, string ProcessName, string TitlePattern, string TypedText, string Command, string ConditionText, int? SubFlowId, int? FlowAreaId, int? FlowPointId);

        public record StepDetail(int Id, int FlowId, string Name, string Type, Dictionary<string, object?> Settings, AreaSummary? Area, List<TemplateSummary> Templates);

        public record TemplateSummary(int Id, string Name, bool IsRequired, float Accuracy, int ClickOffsetX, int ClickOffsetY, int AuthoredFlowAreaWidth, int AuthoredFlowAreaHeight, int AuthoredDpi);

        public record RunSummary(int Id, int FlowId, string FlowName, string Status, DateTime StartedOn, int StepCount, string ErrorMessage);

        public record RunStepSummary(int Sequence, int Depth, string Name, string Type, string Outcome, bool EndedRun, bool WasHandled, int DurationMilliseconds, string? Value, string? Message, int? ExitCode, float? BestScore, string? ClosestTemplate, float? ClosestTemplateAccuracy);

        private sealed record RunStep(int Sequence, int Depth, string Name, string Type, StepOutcomeEnum Outcome, int? FlowStepId, int DurationMilliseconds, string? Value, string? Message, int? ExitCode, float? BestScore, int? BestTemplateId);

        public record StepTypeCount(string Type, int Count);

        public record FlowCheckSummary(int FlowStepId, string Name, string CodeComment, string Type, string? MarkerName, bool IsFatal, string? FailureMessage);

        public record RunOutcomeCount(string Status, int Count);

        public record SettingSummary(string Key, string Label, string Description, string Value, bool IsChanged);

        public record BotSummary(int Id, string Name, string BotName, int RateLimitSeconds);
    }
}
