import { useFlowStepMutations } from "@/features/flow-step/hooks/use-flow-step";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";

interface DisabledChange {
  flowStepId: number;
  isDisabled: boolean;
  parentFlowStepId?: number | null;
  flowId?: number | null;
}

/**
 * Saved at once, whatever a form open on the step is holding, and the branch it sits in is drawn
 * again. The tree's button and the form's both come here.
 */
export function useSetFlowStepDisabled() {
  const { setFlowStepDisabledMutation } = useFlowStepMutations();
  const setTreeRefreshTrigger = useWorkflowStore((x) => x.setTreeRefreshTrigger);

  const setDisabled = ({ flowStepId, isDisabled, parentFlowStepId, flowId }: DisabledChange) => {
    setFlowStepDisabledMutation.mutate(
      { flowStepId, isDisabled },
      {
        onSuccess: () => {
          if (parentFlowStepId) setTreeRefreshTrigger({ id: parentFlowStepId, isFlow: false });
          else if (flowId) setTreeRefreshTrigger({ id: flowId, isFlow: true });
        },
      },
    );
  };

  return { setDisabled, isSettingDisabled: setFlowStepDisabledMutation.isPending };
}
