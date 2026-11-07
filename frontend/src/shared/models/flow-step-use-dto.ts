import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

/** A step and the parts of its flow it uses. What the editor's "Used by" lists read. */
export interface FlowStepUseDto {
  flowStepId: number;
  name: string;
  flowStepType: FlowStepTypeEnum;

  flowAreaId?: number | null;
  flowPointId?: number | null;
  flowPointEndId?: number | null;

  /** Every {{name}} in its text: an input's or another step's. */
  variableNames: string[];
  templateNames: string[];
}
