using System.ComponentModel;
using Core.Enums;
using Core.Helpers;
using Core.Helpers;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Services.Ai.Tools
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
    /// </summary>
    public sealed class DbQueryTools
    {
        private const int _maxRows = 50;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DbQueryTools(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
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

        [Description("One flow with the areas and points it defines. Areas say which application or monitor the flow works against.")]
        public async Task<FlowDetail?> GetFlow([Description("The flow id, from SearchFlows.")] int flowId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            FlowDetail? flow = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == flowId)
                .Select(x => new FlowDetail(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.IsSubFlow,
                    x.FlowSteps.Count(),
                    x.FlowAreas.Select(a => new AreaSummary(
                        a.Id,
                        a.Name,
                        a.Type.ToString(),
                        a.ProcessName,
                        a.TitlePattern,
                        a.MonitorUniqueId,
                        a.Width,
                        a.Height)).ToList(),
                    x.FlowPoints.Select(p => new PointSummary(p.Id, p.Name, p.LocationX, p.LocationY)).ToList()))
                .FirstOrDefaultAsync();

            return flow;
        }

        [Description("The steps of one flow, in tree order. Optionally filtered to one step type.")]
        public async Task<IReadOnlyList<StepSummary>> GetFlowSteps(
            [Description("The flow id.")] int flowId,
            [Description("Optional step type, for example IMAGE_SEARCH, READ_TEXT, CURSOR_CLICK, KEYBOARD_INPUT, SYSTEM_COMMAND. Empty returns every step.")] string flowStepType)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await Steps(dbContext)
                .Where(x => x.FlowId == flowId || x.RootId == flowId)
                .Where(x => flowStepType == "" || x.FlowStepType.ToString() == flowStepType)
                .OrderBy(x => x.ParentFlowStepId)
                .ThenBy(x => x.OrderNumber)
                .Take(_maxRows * 4)
                .Select(Projection())
                .ToListAsync();
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

            return await Steps(dbContext)
                .Where(x => flowStepType == "" || x.FlowStepType.ToString() == flowStepType)
                .Where(x => tokens.Any(t =>
                    EF.Functions.Like(x.Name, t) ||
                    EF.Functions.Like(x.ProcessName, t) ||
                    EF.Functions.Like(x.TitlePattern, t) ||
                    EF.Functions.Like(x.KeyboardInputText, t) ||
                    EF.Functions.Like(x.RunCommandValue, t) ||
                    EF.Functions.Like(x.ConditionText, t)))
                .Take(_maxRows)
                .Select(Projection())
                .ToListAsync();
        }

        [Description("Everything one step is configured with - only the settings its type actually uses, plus its area and templates where it has them. Use this before suggesting why a step misbehaves.")]
        public async Task<StepDetail?> GetFlowStepDetail([Description("The step id, from GetFlowSteps or SearchSteps.")] int flowStepId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            Core.Models.Database.FlowStep? step = await dbContext.FlowSteps
                .AsNoTracking()
                .Include(x => x.FlowStepImages)
                .Include(x => x.FlowArea)
                .FirstOrDefaultAsync(x => x.Id == flowStepId);

            if (step == null)
                return null;

            // Only the columns this type uses. A WAIT step carrying an accuracy and an OCR language
            // reads as though both mean something, and fifty nulls cost context for nothing.
            Dictionary<string, object?> settings = new Dictionary<string, object?>();

            foreach (string field in FlowStepFieldCatalog.FieldsFor(step.FlowStepType))
            {
                object? value = typeof(Core.Models.Database.FlowStep).GetProperty(field)?.GetValue(step);
                settings[field] = value is Enum ? value.ToString() : value;
            }

            AreaSummary? area = step.FlowArea == null
                ? null
                : new AreaSummary(
                    step.FlowArea.Id,
                    step.FlowArea.Name,
                    step.FlowArea.Type.ToString(),
                    step.FlowArea.ProcessName,
                    step.FlowArea.TitlePattern,
                    step.FlowArea.MonitorUniqueId,
                    step.FlowArea.Width,
                    step.FlowArea.Height);

            List<TemplateSummary> templates = step.FlowStepImages
                .Select(x => new TemplateSummary(
                    x.Id,
                    x.Name,
                    x.IsRequired,
                    x.Accuracy,
                    x.AllowMultiScale,
                    x.AuthoredFrameWidth,
                    x.AuthoredFrameHeight))
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

        [Description("The steps of one run, in the order they happened. Indent by depth to read it as a tree.")]
        public async Task<IReadOnlyList<RunStepSummary>> GetRunSteps([Description("The run id, from GetRuns.")] int executionId)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == executionId)
                .OrderBy(x => x.Sequence)
                .Take(_maxRows * 4)
                .Select(x => new RunStepSummary(
                    x.Sequence,
                    x.Depth,
                    x.Name,
                    x.FlowStepType.ToString(),
                    x.Outcome.ToString(),
                    x.DurationMilliseconds,
                    x.Value,
                    x.Message,
                    x.ExitCode))
                .ToListAsync();
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

        public record AreaSummary(int Id, string Name, string Type, string ProcessName, string TitlePattern, string MonitorUniqueId, int Width, int Height);

        public record PointSummary(int Id, string Name, int X, int Y);

        public record StepSummary(int Id, int FlowId, string Name, string Type, string ProcessName, string TitlePattern, string TypedText, string Command, string ConditionText, int? SubFlowId, int? FlowAreaId, int? FlowPointId);

        public record StepDetail(int Id, int FlowId, string Name, string Type, Dictionary<string, object?> Settings, AreaSummary? Area, List<TemplateSummary> Templates);

        public record TemplateSummary(int Id, string Name, bool IsRequired, float? Accuracy, bool AllowMultiScale, int AuthoredFrameWidth, int AuthoredFrameHeight);

        public record RunSummary(int Id, int FlowId, string FlowName, string Status, DateTime StartedOn, int StepCount, string ErrorMessage);

        public record RunStepSummary(int Sequence, int Depth, string Name, string Type, string Outcome, int DurationMilliseconds, string? Value, string? Message, int? ExitCode);

        public record StepTypeCount(string Type, int Count);

        public record RunOutcomeCount(string Status, int Count);

        public record SettingSummary(string Key, string Label, string Description, string Value, bool IsChanged);

        public record BotSummary(int Id, string Name, string BotName, int RateLimitSeconds);
    }
}
