using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// Shape and brightness becomes Shape and colour, whose accuracy is the shape score. A template
    /// on the old default, 0.95, moves to the new one, 0.8. One set stricter than that keeps a strict
    /// shape bar, 0.95: it was raised for something the colour may not see, such as a small badge.
    /// </summary>
    public partial class ShapeAndColour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE FlowStepTemplates
                SET Accuracy = CASE WHEN Accuracy > 0.951 THEN 0.95 ELSE 0.8 END
                WHERE FlowStepId IN (SELECT Id FROM FlowSteps WHERE TemplateMatchMode = 'SHAPE_AND_BRIGHTNESS');");

            migrationBuilder.Sql(@"
                UPDATE FlowSteps
                SET TemplateMatchMode = 'SHAPE_AND_COLOUR'
                WHERE TemplateMatchMode = 'SHAPE_AND_BRIGHTNESS';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE FlowStepTemplates
                SET Accuracy = CASE WHEN Accuracy > 0.9 THEN 0.98 ELSE 0.95 END
                WHERE FlowStepId IN (SELECT Id FROM FlowSteps WHERE TemplateMatchMode = 'SHAPE_AND_COLOUR');");

            migrationBuilder.Sql(@"
                UPDATE FlowSteps
                SET TemplateMatchMode = 'SHAPE_AND_BRIGHTNESS'
                WHERE TemplateMatchMode = 'SHAPE_AND_COLOUR';");
        }
    }
}
