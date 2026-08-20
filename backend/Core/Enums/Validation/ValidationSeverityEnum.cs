namespace Core.Enums
{
    public enum ValidationSeverityEnum
    {
        /// <summary>Structurally broken. The flow cannot run until it is fixed.</summary>
        ERROR,

        /// <summary>Runs, but probably not the way the author meant.</summary>
        WARNING,
    }
}
