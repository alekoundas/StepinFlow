namespace Business.FlowScript.Models.Text
{
    /// <summary>
    /// One keyword mapped to multiple Enums.
    ///
    /// <b>Never compare these with <c>==</c>.</b> 
    /// The static type is <see cref="Enum"/>, which is a class, so <c>==</c> compares objects.
    /// </summary>
    internal sealed class ScriptKeyword
    {
        public string Text { get; }
        public Enum Type { get; }
        public Enum? Modifier { get; }

        /// <summary>Opens a line whose rest is its text, as written: a comment, a stage, the flow's name and id.</summary>
        public bool TakesRestOfLine { get; init; }

        public ScriptKeyword(string text, Enum type, Enum? modifier = null)
        {
            Text = text;
            Type = type;
            Modifier = modifier;
        }

        /// <summary>
        /// Get typed Enum value of "Type" or "Modifier" or null.
        /// </summary>
        public TEnum? As<TEnum>() where TEnum : struct, Enum
        {
            if (Type is TEnum onType)
                return onType;

            if (Modifier is TEnum onModifier)
                return onModifier;

            return null;
        }
    }
}
