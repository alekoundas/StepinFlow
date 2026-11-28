import type { AiChatMessageDto } from "@/shared/models/ai-chat/ai-chat-message-dto";

export interface AiChatRequestDto {
  /** Chosen here. Every event about this answer carries it, and Stop names it. */
  answerId: string;
  messages: AiChatMessageDto[];

  /** What the user is looking at, so "this" means something. An execution says its own flow. */
  flowId?: number;
  executionId?: number;
  flowStepId?: number;

  /** Show the model the pictures executionId kept. They go with this question only. */
  sendScreenshots: boolean;
}
