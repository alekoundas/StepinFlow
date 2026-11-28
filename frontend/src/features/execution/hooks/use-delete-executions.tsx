import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import { ExecutionHooks } from "@/features/execution/hooks/use-execution";
import { ExecutionsFormat } from "@/features/execution/components/executions/executions-format";

/**
 * The two deletes on the executions page, each asked first. The backend hides what it deletes at
 * once, however big; the rows and screenshots wait for the cleanup on Home.
 */
export function useDeleteExecutions() {
  const { openConfirm } = useDialogStore();
  const { deleteExecutionMutation, deleteFlowExecutionsMutation } = ExecutionHooks.useExecutionMutations();

  const deleteOne = (execution: ExecutionFeedItemDto) => {
    const when = `${ExecutionsFormat.dayOf(execution.createdOn)} at ${ExecutionsFormat.timeOf(execution.createdOn)}`;

    openConfirm("execution-delete", {
      headerText: `Delete execution #${execution.id}?`,
      confirmLabel: "Delete the execution",
      confirmIcon: "pi pi-trash",
      confirmSeverity: "danger",
      children: (
        <LabelComponent
          text={`${execution.flowName}, ${when}, ${ExecutionsFormat.stepsText(execution.stepCount)}. Its steps and screenshots go with it. This cannot be undone.`}
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
