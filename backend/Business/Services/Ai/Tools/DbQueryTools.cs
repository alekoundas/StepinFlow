using System.ComponentModel;
using Core.Enums;
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
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Flows
                .AsNoTracking()
                .Where(x => text == "" || EF.Functions.Like(x.Name, $"%{text}%") || EF.Functions.Like(x.Description, $"%{text}%"))
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
            if (string.IsNullOrWhiteSpace(text))
                return [];

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            string like = $"%{text}%";

            return await Steps(dbContext)
                .Where(x => flowStepType == "" || x.FlowStepType.ToString() == flowStepType)
                .Where(x =>
                    EF.Functions.Like(x.Name, like) ||
                    EF.Functions.Like(x.ProcessName, like) ||
                    EF.Functions.Like(x.TitlePattern, like) ||
                    EF.Functions.Like(x.KeyboardInputText, like) ||
                    EF.Functions.Like(x.RunCommand, like) ||
                    EF.Functions.Like(x.RunCommandPresetValue, like) ||
                    EF.Functions.Like(x.ConditionText, like))
                .Take(_maxRows)
                .Select(Projection())
                .ToListAsync();
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
                x.RunCommand,
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

        public record RunSummary(int Id, int FlowId, string FlowName, string Status, DateTime StartedOn, int StepCount, string ErrorMessage);

        public record RunStepSummary(int Sequence, int Depth, string Name, string Type, string Outcome, int DurationMilliseconds, string? Value, string? Message, int? ExitCode);

        public record SettingSummary(string Key, string Label, string Description, string Value, bool IsChanged);

        public record BotSummary(int Id, string Name, string BotName, int RateLimitSeconds);
    }
}
