import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { useFlowMutations } from "@/features/flow/hooks/use-flow";
import type { FlowDto } from "@/shared/models/database/flow-dto";

export function useDeleteFlow() {
  const { openConfirm } = useDialogStore();
  const { deleteFlowMutation } = useFlowMutations();

  return (flow: FlowDto) => {
    const noun = flow.isSubFlow ? "sub-flow" : "flow";

    const size =
      flow.stepCount === 0
        ? "It has no steps."
        : `Its ${flow.stepCount} step${flow.stepCount === 1 ? "" : "s"} go with it.`;

    const used =
      flow.isSubFlow && flow.callerCount > 0
        ? ` ${flow.callerCount} flow${flow.callerCount === 1 ? "" : "s"} run it, and will stop being able to.`
        : "";

    openConfirm("flow-delete", {
      headerText: `Delete ${flow.name}?`,
      confirmLabel: `Delete the ${noun}`,
      confirmSeverity: "danger",
      children: (
        <LabelComponent text={`${size}${used} This cannot be undone.`} />
      ),
      onConfirm: () => deleteFlowMutation.mutateAsync(flow.id),
    });
  };
}
