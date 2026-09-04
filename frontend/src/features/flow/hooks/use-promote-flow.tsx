import { useQueryClient } from "@tanstack/react-query";

import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";

/**
 * Turn a flow into a sub-flow, which is how sub-flows come to exist.
 *
 * One way, so the confirmation has to say both that it sticks and where the flow is going -
 * "my flow disappeared" is the shape the mistake takes.
 */
export function usePromoteFlow() {
  const { openConfirm } = useDialogStore();
  const queryClient = useQueryClient();

  const showError = (message: string) =>
    openConfirm("flow-promote-error", {
      headerText: "It could not be made a sub-flow",
      hideConfirm: true,
      cancelLabel: "Close",
      children: <LabelComponent text={message} />,
    });

  return (flowId: number) =>
    openConfirm("flow-promote", {
      headerText: "Make this a sub-flow?",
      confirmLabel: "Make it a sub-flow",
      confirmSeverity: "warning",
      children: (
        <LabelComponent text="It moves out of Flows and into Sub-Flows, and other flows will be able to run it as a step. This cannot be undone." />
      ),
      onConfirm: async () => {
        try {
          await backendApiService.Flow.promoteToSubFlow(flowId);
          await queryClient.invalidateQueries({ queryKey: ["flow"] });
          await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
        } catch (err) {
          showError(err instanceof Error ? err.message : String(err));
        }
      },
    });
}
