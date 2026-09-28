using Core.Enums;

namespace Business.FlowScript.Models
{
    internal sealed class ScriptKeyword
    {
        public string Text { get; set; }
        public FlowStepTypeEnum Type { get; set; }
        public SearchModeEnum? SearchMode { get; set; }
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }
        public RunCommandPresetEnum? RunCommandPreset { get; set; }

        public ScriptKeyword(
            string text,
            FlowStepTypeEnum type,
            SearchModeEnum? searchMode = null,
            KeyboardInputTypeEnum? keyboardInputType = null,
            RunCommandPresetEnum? runCommandPreset = null)
        {
            Text = text;
            Type = type;
            SearchMode = searchMode;
            KeyboardInputType = keyboardInputType;
            RunCommandPreset = runCommandPreset;
        }
    }
}
