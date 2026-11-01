using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>
    /// Rows written as SQL, for a migration test whose tables are older than the model: EF would
    /// write every column the model has, and the table does not have them yet. Only the id is read
    /// back, which every version of a table has.
    /// </summary>
    internal static class OldRows
    {
        public static int Area(AppDbContext db, int flowId, string name)
        {
            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowAreas (Name, Type, SizingMode, LocationX, LocationY, Width, Height, RatioX, RatioY, RatioWidth, RatioHeight,
                    ProcessName, TitlePattern, TitleMatchMode, UseClientArea, TabMatchValue, TabMatchOn, MonitorDeviceName, FlowId,
                    CreatedOn, UpdatedOn, AuthoredDpi, IsMain)
                VALUES ({0}, 'APPLICATION', 'ABSOLUTE_PX', 0, 0, 0, 0, 0, 0, 0, 0,
                    'app', '', 'CONTAINS', 1, '', 'URL', '', {1},
                    '2026-11-01', '2026-11-01', 0, 0);", name, flowId);

            return db.FlowAreas.Max(x => x.Id);
        }

        /// <summary>A step at the top of the flow when it has no parent, under its parent when it has one.</summary>
        public static int Step(AppDbContext db, int flowId, int? parentFlowStepId, string flowStepType, string name, int orderNumber,
            int? flowAreaId = null, string runCommandPreset = "CUSTOM", string runCommandValue = "")
        {
            int? topLevelFlowId = null;
            if (parentFlowStepId == null)
                topLevelFlowId = flowId;

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowSteps (FlowId, RootId, ParentFlowStepId, FlowAreaId, FlowStepType, Name, OrderNumber, RunCommandPreset, RunCommandValue,
                    CodeComment, ConditionText, ConditionTextEnd, CreatedOn, EndExecutionAsSuccess, IsLoopInfinite, KeyboardInputText,
                    LoopCount, MaxMatches, Message, OcrLanguage, PollIntervalMilliseconds, ResultExtractPattern, ResultSource,
                    RunCommandShell, RunCommandWorkingDirectory, SearchMode, SuccessExitCodes, SystemActionType, TemplateMatchMode,
                    TimeoutMilliseconds, WaitForMilliseconds, WaitForMillisecondsMax, WindowHeight, WindowWidth)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8},
                    '', '', '', '2026-11-01', 0, 0, '',
                    0, 0, '', '', 0, '', 'STDOUT',
                    'CMD', '', 'FIND_BEST', '0', 'LOCK_WORKSTATION', 'SHAPE',
                    0, 0, 0, 0, 0);",
                topLevelFlowId, flowId, parentFlowStepId, flowAreaId, flowStepType, name, orderNumber, runCommandPreset, runCommandValue);

            return db.FlowSteps.Max(x => x.Id);
        }
    }
}
