namespace Core.Enums
{
    public enum BroadcastTypeEnum
    {
        LOG,
        HEALTH,
        OVERLAY_MOUSE_EVENT,
        POINT_CAPTURE_EVENT,

        /// <summary>One coalesced-ready action during a flow recording. Never carries pixels.</summary>
        RECORDING_EVENT,
    }
}
