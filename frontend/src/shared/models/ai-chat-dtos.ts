/** One turn. The page keeps the whole conversation and resends it, so the backend holds no session. */
export interface AiChatMessageDto {
  role: "user" | "assistant";
  text: string;
}

export interface AiChatRequestDto {
  messages: AiChatMessageDto[];
}

export interface AiChatAnswerDto {
  answer: string;

  /** What it looked at, so the reply can be checked rather than trusted. */
  toolCalls: string[];

  error: string;
}

export interface AiChatAvailabilityDto {
  isAvailable: boolean;
  model: string;
  reason: string;
}
