import type { AiLocalModelStateEnum } from "@/shared/enums/backend/ai/ai-local-model-state-enum";

/** The local model, which every AI feature needs whatever cloud model is set. */
export interface AiLocalModelStatusDto {
  state: AiLocalModelStateEnum;
  model: string;

  /** Zero unless it is in memory. */
  memoryBytes: number;

  /** The cloud model that answers in the local model's place. Empty when none does. */
  cloudModel: string;
}
