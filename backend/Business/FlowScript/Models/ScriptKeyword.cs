namespace Business.FlowScript.Models
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

        public ScriptKeyword(string text, Enum type, Enum? modifier = null)
        {
            Text = text;
            Type = type;
            Modifier = modifier;
        }

        /// <summary>Whether this keyword member matches the Type Enum.</summary>
        public bool TypeIs<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return Type.Equals(value);
        }

        /// <summary>Whether this keyword member matches the Modifier Enum.</summary>
        public bool ModifierIs<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return Modifier != null && Modifier.Equals(value);
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
