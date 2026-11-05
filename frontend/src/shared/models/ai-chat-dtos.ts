import type { AiChatEventTypeEnum } from "@/shared/enums/backend/ai/ai-chat-event-type-enum";

/** One turn. The page keeps the whole conversation and resends it, so the backend holds no session. */
export interface AiChatMessageDto {
  role: "user" | "assistant";
  text: string;
}

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

export interface AiChatStepReferenceDto {
  flowStepId: number;

  /** The flow it belongs to. A sub-flow's step opens the sub-flow. */
  flowId: number;

  /** As the model read it, which is what its answer calls it. */
  name: string;
}

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

export interface AiChatAvailabilityDto {
  isAvailable: boolean;
  model: string;
  reason: string;
  isCloud: boolean;
  canSendScreenData: boolean;
}
