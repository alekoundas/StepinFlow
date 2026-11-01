using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NameUnnamedSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An import or a recording saved an End, Notify, Launch, command or sub-flow step with no
            // name: its script line had none. Each gets what an import names it now, numbered within
            // its flow from the second on.
            migrationBuilder.Sql(@"
                WITH Named AS (
                    SELECT s.Id, s.RootId,
                        CASE
                            WHEN s.FlowStepType = 'END_EXECUTION' AND s.EndExecutionAsSuccess = 1 THEN 'End as passed'
                            WHEN s.FlowStepType = 'END_EXECUTION' THEN 'End as failed'
                            WHEN s.FlowStepType = 'NOTIFY' THEN 'Notify'
                            WHEN s.FlowStepType = 'SUB_FLOW' THEN COALESCE(NULLIF(TRIM(f.Name), ''), 'Sub-flow')
                            WHEN s.RunCommandPreset = 'LAUNCH_APP' AND a.Name IS NOT NULL THEN 'Launch ' || a.Name
                            WHEN s.RunCommandPreset = 'LAUNCH_APP' THEN 'Launch an app'
                            WHEN s.RunCommandPreset = 'KILL_PROCESS' THEN 'Kill a process'
                            WHEN s.RunCommandPreset = 'IS_PROCESS_RUNNING' THEN 'Is a process running'
                            WHEN s.RunCommandPreset = 'READ_CLIPBOARD' THEN 'Read clipboard'
                            WHEN s.RunCommandPreset = 'WRITE_CLIPBOARD' THEN 'Write clipboard'
                            WHEN s.RunCommandPreset = 'CHECK_INTERNET' THEN 'Check internet'
                            WHEN s.RunCommandPreset = 'SHUTDOWN_IN' THEN 'Shut down in'
                            WHEN s.RunCommandPreset = 'CANCEL_SHUTDOWN' THEN 'Cancel shutdown'
                            ELSE 'Command'
                        END AS Base
                    FROM FlowSteps s
                    LEFT JOIN Flows f ON f.Id = s.SubFlowId
                    LEFT JOIN FlowAreas a ON a.Id = s.FlowAreaId
                    WHERE TRIM(s.Name) = '' AND s.FlowStepType IN ('END_EXECUTION', 'NOTIFY', 'SYSTEM_COMMAND', 'SUB_FLOW')
                ),
                Numbered AS (
                    SELECT Id, Base, ROW_NUMBER() OVER (PARTITION BY RootId, Base ORDER BY Id) AS N FROM Named
                )
                UPDATE FlowSteps
                SET Name = (SELECT Base || CASE WHEN N = 1 THEN '' ELSE ' ' || N END FROM Numbered WHERE Numbered.Id = FlowSteps.Id)
                WHERE Id IN (SELECT Id FROM Numbered);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Which rows had no name is not kept, and a named step is what a form always made.
        }
    }
}
