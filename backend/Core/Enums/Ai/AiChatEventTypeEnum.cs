namespace Core.Enums
{
    /// <summary>What just happened to an answer being written, as AI_CHAT_EVENT says it.</summary>
    public enum AiChatEventTypeEnum
    {
        STARTED,
        TOOL_STARTED,
        TOOL_FINISHED,
        TEXT,
        DONE,
        STOPPED,
        FAILED,
    }
}
