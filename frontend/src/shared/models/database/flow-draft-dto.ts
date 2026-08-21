import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { DraftStepSourceEnum } from "@/shared/enums/backend/draft-step-source-enum";
import type { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

/**
 * A batch of proposed steps and where they land. The recorder builds it today and the AI will
 * build it later, so nothing below the wizard knows which one it came from.
 */
export interface FlowDraftDto {
  target: FlowDraftTargetDto;
  steps: DraftStepDto[];
}

export interface FlowDraftTargetDto {
  targetFlowId?: number | null;
  targetParentFlowStepId?: number | null;
  targetIndex: number;

  /** Set instead of targetFlowId to create the flow and drop the steps into it. */
  newFlowName?: string | null;
}

export interface DraftStepDto {
  tempId: number;
  parentTempId?: number | null;
  parentBranch?: FlowStepTypeEnum | null;

  values: FlowStepDto;

  /** A point the save creates and links, because a cursor step reads its position from one. */
  newPoint?: DraftPointDto | null;
  newPointEnd?: DraftPointDto | null;

  unresolved: DraftIssueDto[];
  source: DraftStepSourceEnum;
  evidence?: DraftEvidenceDto | null;
}

export interface DraftPointDto {
  name: string;
  locationX: number;
  locationY: number;
}

export interface DraftEvidenceDto {
  /** Key into the recording session screenshot store. */
  screenshotIndex?: number | null;
  windowTitle?: string | null;
  summary: string;
}

export interface DraftIssueDto {
  code: string;
  severity: ValidationSeverityEnum;
  message: string;
}

export interface FlowDraftResultDto {
  flowId: number;
  firstFlowStepId: number;
  createdCount: number;
}
