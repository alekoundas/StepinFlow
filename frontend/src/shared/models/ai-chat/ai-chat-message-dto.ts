/** One turn. The page keeps the whole conversation and resends it, so the backend holds no session. */
export interface AiChatMessageDto {
  role: "user" | "assistant";
  text: string;
}
