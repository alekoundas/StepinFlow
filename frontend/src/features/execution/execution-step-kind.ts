import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";
import { FlowStepCatalog } from "@/shared/models/flow-step-catalog";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

/** A step that routes on its outcome: it either found what it looked for or went to its Failure. */
function isCheckStep(step: ExecutionStepDto): boolean {
  return FlowStepCatalog.hasBranches(step.flowStepType) && !isServedHit(step);
}

/** A hit handed out from an earlier search's screenshot. Nothing executed, so it carries no time. */
function isServedHit(step: ExecutionStepDto): boolean {
  return step.matchIndex !== null && step.matchIndex !== undefined && step.matchIndex > 0;
}

/** Two passes are of one loop when they sit at one depth and come from one step. */
function isSameLoop(first: ExecutionStepDto, other: ExecutionStepDto): boolean {
  if (other.flowStepType !== FlowStepTypeEnum.LOOP || other.depth !== first.depth) return false;

  // Steps deleted since keep their rows with no step to point at.
  if (first.flowStepId) return other.flowStepId === first.flowStepId;
  return other.name === first.name;
}

export const ExecutionStepKind = {
  isCheckStep,
  isServedHit,
  isSameLoop,
};
