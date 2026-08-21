using Core.Enums;
using System.Globalization;

namespace Core.Models.Business
{
    /// <summary>
    /// What a setting is, in one place: its key, its type, its default and its bounds. Adding a
    /// setting is a definition plus an enum member, never a migration.
    /// </summary>
    public abstract class AppSettingDefinition
    {
        protected AppSettingDefinition(AppSettingKeyEnum key, string label, string description)
        {
            Key = key;
            Label = label;
            Description = description;
        }

        public AppSettingKeyEnum Key { get; }
        public string Label { get; }
        public string Description { get; }

        public abstract string DefaultAsText { get; }

        /// <summary>Bounds for the UI to render, null when the setting is not numeric.</summary>
        public virtual int? Minimum => null;
        public virtual int? Maximum => null;
    }

    public sealed class IntAppSettingDefinition : AppSettingDefinition
    {
        public IntAppSettingDefinition(
            AppSettingKeyEnum key,
            string label,
            string description,
            int defaultValue,
            int minimum,
            int maximum)
            : base(key, label, description)
        {
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
        }

        public int DefaultValue { get; }
        public override int? Minimum { get; }
        public override int? Maximum { get; }

        public override string DefaultAsText => DefaultValue.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Out of range and unparsable both fall back rather than throw: a setting is never worth
        /// failing a recording over, and clamping keeps a hand edited row usable.
        /// </summary>
        public int Parse(string? text) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? Math.Clamp(value, Minimum!.Value, Maximum!.Value)
                : DefaultValue;

        public string ToText(int value) =>
            Math.Clamp(value, Minimum!.Value, Maximum!.Value).ToString(CultureInfo.InvariantCulture);
    }
}
