import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { useFlowStepMutations } from "@/features/flow-step/hooks/use-flow-step";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

/**
 * Delete a step, after saying what else goes with it.
 *
 * A step is a branch rather than a row, and the database cascade takes the whole branch - children,
 * their children, all the way down. The tree cannot show that: a collapsed step holding twenty
 * looks exactly like one holding none, so the count is asked for rather than read off the node.
 */
export function useDeleteFlowStep() {
  const { openConfirm } = useDialogStore();
  const { deleteFlowStepMutation } = useFlowStepMutations();

  return async (node: TreeNodeDto, onDeleted: () => void) => {
    // A failed count must not block the delete, so it falls back to the direct children the tree
    // already knows about.
    const impact = await backendApiService.FlowStep.getDeleteImpact(
      node.entityId,
    ).catch(() => null);

    const descendants = impact?.descendantCount ?? node.detail?.childCount ?? 0;
    const referencing = impact?.referencingStepCount ?? 0;

    const below =
      descendants === 0
        ? "Nothing sits under it."
        : `Everything under it goes too - ${descendants} step${descendants === 1 ? "" : "s"}, including anything nested inside them.`;

    // The quiet one. These steps keep running afterwards, without the result they were reading.
    const used =
      referencing === 0
        ? ""
        : ` ${referencing} other step${referencing === 1 ? "" : "s"} read its result and will stop being able to.`;

    openConfirm("flow-step-delete", {
      headerText: `Delete ${node.name || "this step"}?`,
      confirmLabel: "Delete the step",
      confirmSeverity: "danger",
      children: <LabelComponent text={`${below}${used} This cannot be undone.`} />,
      onConfirm: async () => {
        await deleteFlowStepMutation.mutateAsync(node.entityId);
        onDeleted();
      },
    });
  };
}
