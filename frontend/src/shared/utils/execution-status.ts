import type { StatusPillSeverity } from "@/shared/components/StatusPillComponent";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";

/** An execution's state as a pill reads it, the same wherever an execution is listed. */
export function executionStatusSeverity(
  status: ExecutionStatusEnum,
): StatusPillSeverity {
  switch (status) {
    case ExecutionStatusEnum.RUNNING:
      return "running";
    case ExecutionStatusEnum.COMPLETED:
      return "success";
    case ExecutionStatusEnum.FAILED:
    case ExecutionStatusEnum.ERRORED:
      return "danger";
    case ExecutionStatusEnum.INCONCLUSIVE:
      return "warning";
    // Stopped by you or by the app closing: neither is a verdict, so neither wears one's colour.
    default:
      return "neutral";
  }
}

export function executionStatusText(status: ExecutionStatusEnum): string {
  switch (status) {
    case ExecutionStatusEnum.RUNNING:
      return "Executing";
    case ExecutionStatusEnum.COMPLETED:
      return "Completed";
    case ExecutionStatusEnum.FAILED:
      return "Failed";
    case ExecutionStatusEnum.ERRORED:
      return "Errored";
    case ExecutionStatusEnum.INCONCLUSIVE:
      return "Inconclusive";
    case ExecutionStatusEnum.STOPPED:
      return "Stopped";
    default:
      return "Abandoned";
  }
}
