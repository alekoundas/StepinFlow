using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

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

            migrationBuilder.CreateTable(
                name: "Flows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsSubFlow = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    HistoryLevel = table.Column<string>(type: "TEXT", nullable: false),
                    StepCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CsvRowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewportWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewportHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: false),
                    ScreenshotFolderName = table.Column<string>(type: "TEXT", nullable: true),
                    FlowStructureHash = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorFlowStepId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Executions_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFlowAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    SizingMode = table.Column<string>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false),
                    RatioWidth = table.Column<float>(type: "REAL", nullable: false),
                    RatioHeight = table.Column<float>(type: "REAL", nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePattern = table.Column<string>(type: "TEXT", nullable: false),
                    TitleMatchMode = table.Column<string>(type: "TEXT", nullable: false),
                    UseClientArea = table.Column<bool>(type: "INTEGER", nullable: false),
                    TabMatchValue = table.Column<string>(type: "TEXT", nullable: false),
                    TabMatchOn = table.Column<string>(type: "TEXT", nullable: false),
                    MonitorUniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowAreas_FlowAreas_ParentFlowAreaId",
                        column: x => x.ParentFlowAreaId,
                        principalTable: "FlowAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowAreas_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowCsvColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowCsvColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowCsvColumns_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowViewports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowViewports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowViewports_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FlowAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    OffsetMode = table.Column<string>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowPoints_FlowAreas_FlowAreaId",
                        column: x => x.FlowAreaId,
                        principalTable: "FlowAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowPoints_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CodeComment = table.Column<string>(type: "TEXT", nullable: false),
                    FlowStepType = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    WaitForMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    WaitForMillisecondsMax = table.Column<int>(type: "INTEGER", nullable: false),
                    LoopCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLoopInfinite = table.Column<bool>(type: "INTEGER", nullable: false),
                    SearchMode = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateMatchMode = table.Column<string>(type: "TEXT", nullable: false),
                    Accuracy = table.Column<float>(type: "REAL", nullable: false),
                    MaxMatches = table.Column<int>(type: "INTEGER", nullable: false),
                    PollIntervalMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeoutMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    RunCommandShell = table.Column<string>(type: "TEXT", nullable: false),
                    RunCommandPreset = table.Column<string>(type: "TEXT", nullable: false),
                    RunCommandValue = table.Column<string>(type: "TEXT", nullable: false),
                    RunCommandWorkingDirectory = table.Column<string>(type: "TEXT", nullable: false),
                    SuccessExitCodes = table.Column<string>(type: "TEXT", nullable: false),
                    ResultSource = table.Column<string>(type: "TEXT", nullable: false),
                    SystemActionType = table.Column<string>(type: "TEXT", nullable: false),
                    ResultExtractPattern = table.Column<string>(type: "TEXT", nullable: false),
                    OcrLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionText = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionType = table.Column<string>(type: "TEXT", nullable: true),
                    ConditionTextEnd = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePattern = table.Column<string>(type: "TEXT", nullable: false),
                    TitleMatchMode = table.Column<string>(type: "TEXT", nullable: false),
                    WindowHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    WindowWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyboardInputText = table.Column<string>(type: "TEXT", nullable: false),
                    KeyboardInputType = table.Column<string>(type: "TEXT", nullable: true),
                    CursorButtonType = table.Column<string>(type: "TEXT", nullable: true),
                    CursorButtonActionType = table.Column<string>(type: "TEXT", nullable: true),
                    CursorScrollDirectionType = table.Column<string>(type: "TEXT", nullable: true),
                    RootId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubFlowId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiscordBotId = table.Column<int>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    EndExecutionStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowPointId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowPointEndId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentFlowStepId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowStepReferenceId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowStepReferenceEndId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowSteps_DiscordBots_DiscordBotId",
                        column: x => x.DiscordBotId,
                        principalTable: "DiscordBots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowAreas_FlowAreaId",
                        column: x => x.FlowAreaId,
                        principalTable: "FlowAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowPoints_FlowPointEndId",
                        column: x => x.FlowPointEndId,
                        principalTable: "FlowPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowPoints_FlowPointId",
                        column: x => x.FlowPointId,
                        principalTable: "FlowPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSteps_FlowStepReferenceEndId",
                        column: x => x.FlowStepReferenceEndId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                        column: x => x.FlowStepReferenceId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                        column: x => x.ParentFlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSteps_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSteps_Flows_SubFlowId",
                        column: x => x.SubFlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentSequence = table.Column<int>(type: "INTEGER", nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    LoopPass = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FlowStepType = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    StartedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultLocationX = table.Column<int>(type: "INTEGER", nullable: true),
                    ResultLocationY = table.Column<int>(type: "INTEGER", nullable: true),
                    BestScore = table.Column<float>(type: "REAL", nullable: true),
                    MatchIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    Command = table.Column<string>(type: "TEXT", nullable: true),
                    ScreenshotFileName = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowStepId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionSteps_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionSteps_FlowSteps_FlowStepId",
                        column: x => x.FlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FlowStepLastGoodScreenshotHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ViewportWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewportHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowStepLastGoodScreenshotHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowStepLastGoodScreenshotHistories_FlowSteps_FlowStepId",
                        column: x => x.FlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowStepTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateMatchMode = table.Column<string>(type: "TEXT", nullable: true),
                    TemplateImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Accuracy = table.Column<float>(type: "REAL", nullable: true),
                    ClickOffsetX = table.Column<int>(type: "INTEGER", nullable: false),
                    ClickOffsetY = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredFrameWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredFrameHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredMonitorId = table.Column<string>(type: "TEXT", nullable: false),
                    AuthoredMonitorDpi = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowMultiScale = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScaleTolerance = table.Column<float>(type: "REAL", nullable: false),
                    FlowStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowStepTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowStepTemplates_FlowSteps_FlowStepId",
                        column: x => x.FlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_FlowId_CreatedOn",
                table: "Executions",
                columns: new[] { "FlowId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_ExecutionId_Sequence",
                table: "ExecutionSteps",
                columns: new[] { "ExecutionId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_FlowStepId",
                table: "ExecutionSteps",
                column: "FlowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowAreas_FlowId",
                table: "FlowAreas",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowAreas_ParentFlowAreaId",
                table: "FlowAreas",
                column: "ParentFlowAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowCsvColumns_FlowId",
                table: "FlowCsvColumns",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowPoints_FlowAreaId",
                table: "FlowPoints",
                column: "FlowAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowPoints_FlowId",
                table: "FlowPoints",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_Flows_Id",
                table: "Flows",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepLastGoodScreenshotHistories_FlowStepId_ViewportWidth_ViewportHeight",
                table: "FlowStepLastGoodScreenshotHistories",
                columns: new[] { "FlowStepId", "ViewportWidth", "ViewportHeight" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_DiscordBotId",
                table: "FlowSteps",
                column: "DiscordBotId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowAreaId",
                table: "FlowSteps",
                column: "FlowAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowId_OrderNumber",
                table: "FlowSteps",
                columns: new[] { "FlowId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowPointEndId",
                table: "FlowSteps",
                column: "FlowPointEndId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowPointId",
                table: "FlowSteps",
                column: "FlowPointId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowStepReferenceEndId",
                table: "FlowSteps",
                column: "FlowStepReferenceEndId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps",
                column: "FlowStepReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_ParentFlowStepId_OrderNumber",
                table: "FlowSteps",
                columns: new[] { "ParentFlowStepId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_RootId",
                table: "FlowSteps",
                column: "RootId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_SubFlowId",
                table: "FlowSteps",
                column: "SubFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepTemplates_FlowStepId",
                table: "FlowStepTemplates",
                column: "FlowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowViewports_FlowId",
                table: "FlowViewports",
                column: "FlowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "ExecutionSteps");

            migrationBuilder.DropTable(
                name: "FlowCsvColumns");

            migrationBuilder.DropTable(
                name: "FlowStepLastGoodScreenshotHistories");

            migrationBuilder.DropTable(
                name: "FlowStepTemplates");

            migrationBuilder.DropTable(
                name: "FlowViewports");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "FlowSteps");

            migrationBuilder.DropTable(
                name: "DiscordBots");

            migrationBuilder.DropTable(
                name: "FlowPoints");

            migrationBuilder.DropTable(
                name: "FlowAreas");

            migrationBuilder.DropTable(
                name: "Flows");
        }
    }
}
