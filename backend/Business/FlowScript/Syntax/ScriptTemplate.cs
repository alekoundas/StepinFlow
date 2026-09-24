namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// A template a step names, and the accuracy it is searched at. Each template has its own, so
    /// the accuracy is written straight after it rather than once on the step.
    /// </summary>
    public sealed class ScriptTemplate
    {
        // SHAPE's default. The script has no way to say the match mode yet, so every imported step
        // is SHAPE and this is the right starting point.
        public const float DefaultAccuracy = 0.8f;

        public ScriptTemplate(string fileName, float accuracy)
        {
            FileName = fileName;
            Accuracy = accuracy;
        }

        public string FileName { get; }

        public float Accuracy { get; set; }
    }
}
