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

        /// <summary>An area from when a tab was found by its address and a window could be started, up to the borders' rename.</summary>
        public static int AreaWithAddress(AppDbContext db, int flowId, string name, bool useClientArea = true)
        {
            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowAreas (Name, Type, SizingMode, LocationX, LocationY, Width, Height, RatioX, RatioY, RatioWidth, RatioHeight,
                    ProcessName, TitlePattern, TitleMatchMode, UseClientArea, TabAddress, MonitorDeviceName, FlowId,
                    CreatedOn, UpdatedOn, AuthoredDpi, IsMain, LaunchCommand)
                VALUES ({0}, 'APPLICATION', 'ABSOLUTE_PX', 0, 0, 0, 0, 0, 0, 0, 0,
                    'app', '', 'CONTAINS', {1}, '', '', {2},
                    '2026-11-07', '2026-11-07', 0, 0, '');", name, useClientArea, flowId);

            return db.FlowAreas.Max(x => x.Id);
        }

        /// <summary>A step at the top of the flow when it has no parent, under its parent when it has one.</summary>
        public static int Step(AppDbContext db, int flowId, int? parentFlowStepId, string flowStepType, string name, int orderNumber,
            int? flowAreaId = null, string runCommandPreset = "CUSTOM", string runCommandValue = "", int? subFlowId = null, bool endExecutionAsSuccess = false)
        {
            int? topLevelFlowId = null;
            if (parentFlowStepId == null)
                topLevelFlowId = flowId;

            // Added with no default, so written only once the table has it.
            string launchArgumentsColumn = "";
            string launchArgumentsValue = "";
            if (HasColumn(db, "FlowSteps", "LaunchArguments"))
            {
                launchArgumentsColumn = ", LaunchArguments";
                launchArgumentsValue = ", ''";
            }

            // Only the constant column above is joined in; every value is a parameter.
#pragma warning disable EF1003
            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowSteps (FlowId, RootId, ParentFlowStepId, FlowAreaId, FlowStepType, Name, OrderNumber, RunCommandPreset, RunCommandValue,
                    SubFlowId, EndExecutionAsSuccess,
                    CodeComment, ConditionText, ConditionTextEnd, CreatedOn, IsLoopInfinite, KeyboardInputText,
                    LoopCount, MaxMatches, Message, OcrLanguage, PollIntervalMilliseconds, ResultExtractPattern, ResultSource,
                    RunCommandShell, RunCommandWorkingDirectory, SearchMode, SuccessExitCodes, SystemActionType, TemplateMatchMode,
                    TimeoutMilliseconds, WaitForMilliseconds, WaitForMillisecondsMax, WindowHeight, WindowWidth" + launchArgumentsColumn + @")
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8},
                    {9}, {10},
                    '', '', '', '2026-11-01', 0, '',
                    0, 0, '', '', 0, '', 'STDOUT',
                    'CMD', '', 'FIND_BEST', '0', 'LOCK_WORKSTATION', 'SHAPE',
                    0, 0, 0, 0, 0" + launchArgumentsValue + ");",
                topLevelFlowId, flowId, parentFlowStepId, flowAreaId, flowStepType, name, orderNumber, runCommandPreset, runCommandValue,
                subFlowId, endExecutionAsSuccess);
#pragma warning restore EF1003

            return db.FlowSteps.Max(x => x.Id);
        }

        private static bool HasColumn(AppDbContext db, string table, string column)
        {
            return db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info({0}) WHERE name = {1}", table, column)
                .AsEnumerable()
                .Single() > 0;
        }
    }
}
