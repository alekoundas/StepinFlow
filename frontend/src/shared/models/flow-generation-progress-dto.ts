import type { FlowGenerationStageEnum } from "@/shared/enums/backend/flow-generation-stage-enum";
import type { FlowGenerationStateEnum } from "@/shared/enums/backend/flow-generation-state-enum";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";

/** Where the model's draft of a reviewed recording is. Sent whole on every change. */
export interface FlowGenerationProgressDto {
  state: FlowGenerationStateEnum;
  stage: FlowGenerationStageEnum;

  /** The model asked, as Settings names it. */
  model: string;

  /** Whether the text read under each click went to the model. */
  isReadTextSent: boolean;

  /** The draft as a script, in characters, once written. */
  scriptLength: number;

  /** Which attempt, from 1, of attemptCount. 0 before the model is asked. */
  attempt: number;
  attemptCount: number;

  /** Why the last answer was refused, which the model is now asked to fix. */
  refused: FlowScriptErrorDto[];

  /** Set when nothing could be saved at all. */
  errorMessage?: string | null;
}
