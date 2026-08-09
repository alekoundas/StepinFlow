namespace Core.Enums
{
    public enum FlowSearchAreaTypeEnum
    {
        CUSTOM,       // A rectangle, optionally inside another area
        APPLICATION,  // A window, found at runtime
        BROWSER_TAB,  // A browser tab's document area, found through UI Automation
        MONITOR,
    }
}
