import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

/** A flow, or a step and the flow it is in, that the header's search found. */
export interface FlowSearchHitDto {
  flowId: number;
  flowName: string;
  isSubFlow: boolean;
  isQaTest: boolean;

  /** Null when the flow itself matched. */
  flowStepId: number | null;
  flowStepName: string | null;
  flowStepType: FlowStepTypeEnum | null;
}
