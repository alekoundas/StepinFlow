using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordBotsAndNotifyStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscordBotId",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifyMessage",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DiscordBots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookUrl = table.Column<string>(type: "TEXT", nullable: false),
                    BotName = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", nullable: false),
                    RateLimitSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordBots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_DiscordBotId",
                table: "FlowSteps",
                column: "DiscordBotId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_DiscordBots_DiscordBotId",
                table: "FlowSteps",
                column: "DiscordBotId",
                principalTable: "DiscordBots",
                principalColumn: "Id");

            // The email notification type was a placeholder that never had a form, so nothing can
            // have saved one. Converted anyway rather than left as a value the enum can no longer
            // parse.
            migrationBuilder.Sql(@"
                UPDATE FlowSteps SET FlowStepType = 'NOTIFY' WHERE FlowStepType = 'NOTIFICATION_EMAIL';
            ");

            // Window steps now branch, and the tree expects both children to exist. Rows saved
            // before this migration have neither, so they would render as a branching node with
            // nothing under it and nowhere to drop a step. One insert per branch, skipping any
            // step that somehow already has one.
            //
            // The columns listed are every NOT NULL column on FlowSteps. The enum-backed ones are
            // copied from the parent rather than guessed at: their value on a branch node is inert,
            // and copying cannot produce something the enum fails to parse.
            foreach ((string type, string name, int order) in new[]
            {
                ("SUCCESS", "Success", 0),
                ("FAILURE", "Failure", 1),
            })
            {
                migrationBuilder.Sql($@"
                    INSERT INTO FlowSteps (
                        Name, FlowStepType, OrderNumber, RootId, ParentFlowStepId, CreatedOn,
                        ConditionText, ConditionTextEnd, KeyboardInputText, NotifyMessage,
                        OcrLanguage, ResultExtractPattern, RunCommand, RunCommandPresetValue,
                        RunCommandWorkingDirectory, SuccessExitCodes,
                        ResultSource, RunCommandPreset, RunCommandShell, SearchMode,
                        SystemActionType, TemplateMatchMode,
                        Accuracy, LoopCount, MaxMatches, PollIntervalMilliseconds,
                        TimeoutMilliseconds, WaitForMilliseconds, WindowHeight, WindowWidth,
                        IsLoopInfinite, IsPointCustom, IsPointEndCustom
                    )
                    SELECT
                        '{name}', '{type}', {order}, p.RootId, p.Id, p.CreatedOn,
                        '', '', '', '',
                        '', '', '', '',
                        '', '0',
                        p.ResultSource, p.RunCommandPreset, p.RunCommandShell, p.SearchMode,
                        p.SystemActionType, p.TemplateMatchMode,
                        0.8, 0, 20, 500,
                        0, 0, 0, 0,
                        0, 0, 0
                    FROM FlowSteps p
                    WHERE p.FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE')
                      AND NOT EXISTS (
                          SELECT 1 FROM FlowSteps c
                          WHERE c.ParentFlowStepId = p.Id AND c.FlowStepType = '{type}'
                      );
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Window steps had no branch children before this migration, so every SUCCESS or
            // FAILURE row under one was created by it.
            migrationBuilder.Sql(@"
                DELETE FROM FlowSteps
                WHERE FlowStepType IN ('SUCCESS', 'FAILURE')
                  AND ParentFlowStepId IN (
                      SELECT Id FROM FlowSteps
                      WHERE FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE')
                  );
            ");

            migrationBuilder.Sql(@"
                UPDATE FlowSteps SET FlowStepType = 'NOTIFICATION_EMAIL' WHERE FlowStepType = 'NOTIFY';
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_DiscordBots_DiscordBotId",
                table: "FlowSteps");

            migrationBuilder.DropTable(
                name: "DiscordBots");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_DiscordBotId",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "DiscordBotId",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "NotifyMessage",
                table: "FlowSteps");
        }
    }
}
