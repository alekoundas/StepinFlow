using System.Drawing;

using Core.Enums;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// A template a step names, with everything the file says about it.
    ///
    /// Two places say it. The step line carries the decisions about the search - the accuracy and
    /// whether the template is required - and the <c>Templates:</c> header carries the facts about
    /// the picture: where to click, and the size and DPI of the area it was captured in.
    /// </summary>
    public sealed class ScriptTemplate
    {
        public string FileName { get; init; } = string.Empty;

        public float Accuracy { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>Null when the header does not say, and the importer centres it on the picture.</summary>
        public Point? ClickOffset { get; set; }

        public int AuthoredFlowAreaWidth { get; set; }

        public int AuthoredFlowAreaHeight { get; set; }

        public int AuthoredDpi { get; set; }

        /// <summary>
        /// What a template starts on when the file gives it no accuracy. Per mode, because the two
        /// are different instruments and one number cannot mean the same in both.
        /// </summary>
        public static float DefaultAccuracy(TemplateMatchModeEnum mode)
        {
            if (mode == TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS)
                return 0.95f;

            return 0.8f;
        }
    }
}
