import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import { useExecutionMutations } from "@/features/execution/hooks/use-execution";
import { dayOf, stepsText, timeOf } from "@/features/execution/components/executions/executions-format";

/**
 * The two deletes on the executions page, each asked first. The dialog stays until the backend
 * has deleted the rows and handed their space back, which for an execution left on for days is
 * a few seconds.
 */
export function useDeleteExecutions() {
  const { openConfirm } = useDialogStore();
  const { deleteExecutionMutation, deleteFlowExecutionsMutation } = useExecutionMutations();

  const deleteOne = (execution: ExecutionFeedItemDto) => {
    const when = `${dayOf(execution.createdOn)} at ${timeOf(execution.createdOn)}`;

    openConfirm("execution-delete", {
      headerText: `Delete execution #${execution.id}?`,
      confirmLabel: "Delete the execution",
      confirmIcon: "pi pi-trash",
      confirmSeverity: "danger",
      children: (
        <LabelComponent
          text={`${execution.flowName}, ${when}, ${stepsText(execution.stepCount)}. Its steps and screenshots go with it. This cannot be undone.`}
        />
      ),
      onConfirm: () => deleteExecutionMutation.mutateAsync(execution.id),
    });
  };

  // The one going now stays, so it is not counted among those that go.
  const deleteAll = (summary: FlowExecutionSummaryDto, isExecuting: boolean) => {
    let count = summary.runCount;
    if (isExecuting) count -= 1;

    const what = `${count.toLocaleString()} execution${count === 1 ? "" : "s"}`;
    const stays = isExecuting ? " The one executing now stays." : "";

    openConfirm("flow-executions-delete", {
      headerText: `Delete the executions of ${summary.flowName}?`,
      confirmLabel: `Delete ${what}`,
      confirmIcon: "pi pi-trash",
      confirmSeverity: "danger",
      children: (
        <LabelComponent text={`${what}, with their steps and screenshots.${stays} The flow stays. This cannot be undone.`} />
      ),
      onConfirm: () => deleteFlowExecutionsMutation.mutateAsync(summary.flowId),
    });
  };

  return { deleteOne, deleteAll };
}
