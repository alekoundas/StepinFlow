using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AiLocalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One provider becomes a local model every install needs, plus a cloud one on top. An
            // OpenAI model stays the cloud's; any other was Ollama's, and is the local model now.
            migrationBuilder.Sql(@"
                UPDATE AppSettings SET ""Key"" = 'AI_CLOUD_MODEL'
                WHERE ""Key"" = 'AI_MODEL' AND EXISTS (SELECT 1 FROM AppSettings WHERE ""Key"" = 'AI_PROVIDER' AND Value = 'OPENAI');");

            migrationBuilder.Sql(@"UPDATE AppSettings SET ""Key"" = 'AI_OLLAMA_MODEL' WHERE ""Key"" = 'AI_MODEL';");

            migrationBuilder.Sql(@"UPDATE AppSettings SET ""Key"" = 'AI_CLOUD_PROVIDER' WHERE ""Key"" = 'AI_PROVIDER' AND Value = 'OPENAI';");

            migrationBuilder.Sql(@"DELETE FROM AppSettings WHERE ""Key"" = 'AI_PROVIDER';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to one provider: the cloud one when it was chosen, Ollama otherwise.
            migrationBuilder.Sql(@"DELETE FROM AppSettings WHERE ""Key"" = 'AI_CLOUD_PROVIDER' AND Value <> 'OPENAI';");

            migrationBuilder.Sql(@"UPDATE AppSettings SET ""Key"" = 'AI_PROVIDER' WHERE ""Key"" = 'AI_CLOUD_PROVIDER';");

            migrationBuilder.Sql(@"
                DELETE FROM AppSettings
                WHERE ""Key"" = 'AI_OLLAMA_MODEL' AND EXISTS (SELECT 1 FROM AppSettings WHERE ""Key"" = 'AI_PROVIDER');");

            migrationBuilder.Sql(@"
                UPDATE AppSettings SET ""Key"" = 'AI_MODEL'
                WHERE ""Key"" = 'AI_CLOUD_MODEL' AND EXISTS (SELECT 1 FROM AppSettings WHERE ""Key"" = 'AI_PROVIDER');");

            migrationBuilder.Sql(@"DELETE FROM AppSettings WHERE ""Key"" = 'AI_CLOUD_MODEL';");

            migrationBuilder.Sql(@"INSERT INTO AppSettings (""Key"", Value) SELECT 'AI_PROVIDER', 'OLLAMA' FROM AppSettings WHERE ""Key"" = 'AI_OLLAMA_MODEL';");

            migrationBuilder.Sql(@"UPDATE AppSettings SET ""Key"" = 'AI_MODEL' WHERE ""Key"" = 'AI_OLLAMA_MODEL';");
        }
    }
}
