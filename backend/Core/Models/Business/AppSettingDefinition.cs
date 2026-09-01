using Core.Enums;
using System.Globalization;

namespace Core.Models.Business
{
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

        /// <summary>What control the Settings page should render. A number box is not a hotkey.</summary>
        public abstract AppSettingKindEnum Kind { get; }
    }


    public sealed class HotkeyAppSettingDefinition : AppSettingDefinition
    {
        public HotkeyAppSettingDefinition(
            AppSettingKeyEnum key,
            string label,
            string description,
            string defaultCombination)
            : base(key, label, description)
        {
            DefaultCombination = defaultCombination;
        }

        public string DefaultCombination { get; }

        public override AppSettingKindEnum Kind => AppSettingKindEnum.HOTKEY;

        public override string DefaultAsText => DefaultCombination;
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

        public override AppSettingKindEnum Kind => AppSettingKindEnum.INT;

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


    public sealed class TextAppSettingDefinition : AppSettingDefinition
    {
        public TextAppSettingDefinition(
            AppSettingKeyEnum key,
            string label,
            string description,
            string defaultValue = "",
            bool isSecret = false)
            : base(key, label, description)
        {
            DefaultValue = defaultValue;
            IsSecret = isSecret;
        }

        public string DefaultValue { get; }

        /// <summary>An api key. The page shows that one is set, never what it is.</summary>
        public bool IsSecret { get; }

        public override AppSettingKindEnum Kind => IsSecret
            ? AppSettingKindEnum.SECRET
            : AppSettingKindEnum.TEXT;

        public override string DefaultAsText => DefaultValue;
    }

    public sealed class ChoiceAppSettingDefinition : AppSettingDefinition
    {
        public ChoiceAppSettingDefinition(
            AppSettingKeyEnum key,
            string label,
            string description,
            string defaultValue,
            IReadOnlyList<string> options)
            : base(key, label, description)
        {
            DefaultValue = defaultValue;
            Options = options;
        }

        public string DefaultValue { get; }
        public IReadOnlyList<string> Options { get; }

        public override AppSettingKindEnum Kind => AppSettingKindEnum.CHOICE;

        public override string DefaultAsText => DefaultValue;
    }

    public sealed class BoolAppSettingDefinition : AppSettingDefinition
    {
        public BoolAppSettingDefinition(
            AppSettingKeyEnum key,
            string label,
            string description,
            bool defaultValue)
            : base(key, label, description)
        {
            DefaultValue = defaultValue;
        }

        public bool DefaultValue { get; }

        public override AppSettingKindEnum Kind => AppSettingKindEnum.BOOL;

        public override string DefaultAsText => DefaultValue ? "true" : "false";

        /// <summary>Anything unparsable falls back rather than throws, as the int one does.</summary>
        public bool Parse(string? text)
        {
            if (bool.TryParse(text, out bool value))
                return value;

            return DefaultValue;
        }
    }
}
