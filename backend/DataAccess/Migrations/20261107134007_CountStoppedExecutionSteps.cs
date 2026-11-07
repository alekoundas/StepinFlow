using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// A stopped or abandoned execution was saved with 0 steps, whatever it executed: the count was
    /// only written when the walk reached its end. Its history kept every step it executed, so that
    /// is counted instead. One that kept no history has nothing to count, and stays at 0.
    /// </summary>
    public partial class CountStoppedExecutionSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Executions
                SET StepCount = (SELECT COUNT(*) FROM ExecutionSteps WHERE ExecutionSteps.ExecutionId = Executions.Id)
                WHERE StepCount = 0 AND Status IN ('STOPPED', 'ABANDONED');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
