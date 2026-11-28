import type { AiChatEventTypeEnum } from "@/shared/enums/backend/ai/ai-chat-event-type-enum";
import type { AiChatStepReferenceDto } from "@/shared/models/ai-chat/ai-chat-step-reference-dto";

/** One thing that happened to an answer, sent as AI_CHAT_EVENT. */
export interface AiChatEventDto {
  answerId: string;
  type: AiChatEventTypeEnum;

  /** STARTED: the model answering, and the pictures that went with the question. */
  model: string;
  images: string[];

  /** TOOL_STARTED and TOOL_FINISHED: which call, and what it is doing in plain words. */
  activityId: string;
  activity: string;

  /** TOOL_FINISHED: the steps it returned. */
  steps: AiChatStepReferenceDto[];

  /** TEXT: what was added. DONE: the whole answer. */
  text: string;

  /** FAILED: what went wrong, then what it said about it. */
  error: string;
  errorDetail: string;
}
