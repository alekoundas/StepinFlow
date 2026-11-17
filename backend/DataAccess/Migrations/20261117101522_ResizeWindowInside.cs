using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// A resize of an application window whose area leaves out the title bar and borders sets its
    /// inside, not the whole window. Each saved one moves to the inside most of its area's template
    /// images were captured at, when that is a title bar and borders smaller than the window it set;
    /// otherwise to the window less the title bar and borders at 100%. A name that said the size
    /// says the new one.
    /// </summary>
    public partial class ResizeWindowInside : Migration
    {
        // The title bar and borders at 100%: 8 a side, and 31 above.
        private const int FRAME_WIDTH = 16;
        private const int FRAME_HEIGHT = 39;

        // More than at any scaling: 36 x 103 at 300%.
        private const int FRAME_WIDTH_MAX = 64;
        private const int FRAME_HEIGHT_MAX = 160;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                CREATE TEMP TABLE ResizedInside AS
                WITH Captured AS (
                    SELECT resize.Id AS StepId, template.AuthoredFlowAreaWidth AS Width, template.AuthoredFlowAreaHeight AS Height, COUNT(*) AS Uses
                    FROM FlowSteps resize
                    JOIN FlowSteps search ON search.FlowAreaId = resize.FlowAreaId
                    JOIN FlowStepTemplates used ON used.FlowStepId = search.Id
                    JOIN FlowTemplates template ON template.Id = used.FlowTemplateId
                    WHERE resize.FlowStepType = 'WINDOW_RESIZE'
                      AND template.AuthoredFlowAreaWidth > 0 AND template.AuthoredFlowAreaHeight > 0
                      AND resize.WindowWidth - template.AuthoredFlowAreaWidth BETWEEN 0 AND {FRAME_WIDTH_MAX}
                      AND resize.WindowHeight - template.AuthoredFlowAreaHeight BETWEEN 0 AND {FRAME_HEIGHT_MAX}
                    GROUP BY resize.Id, template.AuthoredFlowAreaWidth, template.AuthoredFlowAreaHeight),
                Most AS (
                    SELECT StepId, Width, Height, ROW_NUMBER() OVER (PARTITION BY StepId ORDER BY Uses DESC, Width DESC, Height DESC) AS Place
                    FROM Captured),
                Sized AS (
                    SELECT resize.Id AS StepId,
                        'Resize window to ' || resize.WindowWidth || 'x' || resize.WindowHeight AS OldName,
                        COALESCE(most.Width, MAX(resize.WindowWidth - {FRAME_WIDTH}, 1)) AS Width,
                        COALESCE(most.Height, MAX(resize.WindowHeight - {FRAME_HEIGHT}, 1)) AS Height
                    FROM FlowSteps resize
                    JOIN FlowAreas area ON area.Id = resize.FlowAreaId
                    LEFT JOIN Most most ON most.StepId = resize.Id AND most.Place = 1
                    WHERE resize.FlowStepType = 'WINDOW_RESIZE'
                      AND area.Type = 'APPLICATION' AND area.IgnoreWindowBorders = 1
                      AND resize.WindowWidth > 0 AND resize.WindowHeight > 0)
                SELECT StepId, OldName, Width, Height, 'Resize window to ' || Width || 'x' || Height AS NewName
                FROM Sized;");

            // A numbered one, the second of a name in its flow, keeps its number.
            migrationBuilder.Sql(@"
                UPDATE FlowSteps
                SET WindowWidth = (SELECT Width FROM ResizedInside WHERE StepId = FlowSteps.Id),
                    WindowHeight = (SELECT Height FROM ResizedInside WHERE StepId = FlowSteps.Id),
                    Name = (
                        SELECT CASE
                            WHEN FlowSteps.Name LIKE OldName OR FlowSteps.Name LIKE OldName || ' (%)'
                            THEN NewName || substr(FlowSteps.Name, length(OldName) + 1)
                            ELSE FlowSteps.Name END
                        FROM ResizedInside WHERE StepId = FlowSteps.Id)
                WHERE Id IN (SELECT StepId FROM ResizedInside);");

            migrationBuilder.Sql("DROP TABLE ResizedInside;");
        }

        // The whole window again, as the title bar and borders at 100% make it.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE FlowSteps
                SET Name = CASE
                        WHEN Name LIKE 'Resize window to ' || WindowWidth || 'x' || WindowHeight
                          OR Name LIKE 'Resize window to ' || WindowWidth || 'x' || WindowHeight || ' (%)'
                        THEN 'Resize window to ' || (WindowWidth + {FRAME_WIDTH}) || 'x' || (WindowHeight + {FRAME_HEIGHT})
                            || substr(Name, length('Resize window to ' || WindowWidth || 'x' || WindowHeight) + 1)
                        ELSE Name END,
                    WindowWidth = WindowWidth + {FRAME_WIDTH},
                    WindowHeight = WindowHeight + {FRAME_HEIGHT}
                WHERE FlowStepType = 'WINDOW_RESIZE' AND WindowWidth > 0 AND WindowHeight > 0
                  AND FlowAreaId IN (SELECT Id FROM FlowAreas WHERE Type = 'APPLICATION' AND IgnoreWindowBorders = 1);");
        }
    }
}
