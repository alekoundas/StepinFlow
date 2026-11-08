using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// A text search's language was a Windows OCR tag - en-US, el-GR - and is now the PaddleOCR
    /// reader for its script. A tag in a script with a reader of its own goes to that reader,
    /// Serbian in Cyrillic included; every other tag is written in Latin letters. None picked stays
    /// none, which reads with Latin.
    /// </summary>
    public partial class OcrLanguageIsReader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE FlowSteps
                SET OcrLanguage = CASE
                    WHEN {Tags("el")} THEN 'greek'
                    WHEN {Tags("ru", "uk", "be")} THEN 'eslav'
                    WHEN {Tags("bg", "mk", "mn")} OR lower(OcrLanguage) LIKE 'sr-cyrl%' THEN 'cyrillic'
                    WHEN {Tags("zh", "ja")} THEN 'chinese'
                    WHEN {Tags("ko")} THEN 'korean'
                    WHEN {Tags("ar", "fa", "ur")} THEN 'arabic'
                    WHEN {Tags("hi", "mr", "ne")} THEN 'devanagari'
                    WHEN {Tags("th")} THEN 'thai'
                    WHEN {Tags("ta")} THEN 'tamil'
                    WHEN {Tags("te")} THEN 'telugu'
                    ELSE 'latin'
                END
                WHERE OcrLanguage <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        // A language with any region after it: el, el-GR.
        private static string Tags(params string[] languages)
        {
            IEnumerable<string> each = languages.Select(x => $"lower(OcrLanguage) = '{x}' OR lower(OcrLanguage) LIKE '{x}-%'");
            return "(" + string.Join(" OR ", each) + ")";
        }
    }
}
