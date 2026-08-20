namespace Core.Enums
{
    /// <summary>What a step hands to the steps that reference it.</summary>
    public enum StepResultKindEnum
    {
        /// <summary>A screen position the cursor steps can act on.</summary>
        LOCATION,

        /// <summary>Text a condition can be tested against.</summary>
        VALUE,
    }
}
