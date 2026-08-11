namespace Core.Enums
{
    /// <summary>Which part of a finished command becomes the step's result.</summary>
    public enum ResultSourceEnum
    {
        STDOUT,
        STDERR,
        COMBINED,
        EXIT_CODE,
    }
}
