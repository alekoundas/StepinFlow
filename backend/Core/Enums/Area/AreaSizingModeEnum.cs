namespace Core.Enums
{
    /// <summary>
    /// How an area's offset and size are read. ABSOLUTE_PX is exact and is the normal choice
    /// because a flow resizes its own window; RATIO is for areas whose size cannot be forced.
    /// </summary>
    public enum AreaSizingModeEnum
    {
        ABSOLUTE_PX,
        RATIO,
    }
}
