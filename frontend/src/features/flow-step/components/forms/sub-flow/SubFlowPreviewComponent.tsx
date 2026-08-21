import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Panel } from "primereact/panel";
import type { TreeExpandedKeysType } from "primereact/tree";

import LabelComponent from "@/shared/components/LabelComponent";
import ReadOnlyFlowTreeComponent from "@/shared/components/data-tree/ReadOnlyFlowTreeComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { getFlowStepForm } from "@/features/flow-step/components/forms/flow-step-form-registry";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  flowId: number;
}

/**
 * What the invoked sub-flow does, shown here rather than folded into the caller's tree.
 *
 * Keeping it in the panel is what lets the caller's tree stay single rooted: no foreign nodes to
 * mark undraggable, and nothing from another flow can reach the move machinery, which works
 * strictly within one RootId.
 */
export default function SubFlowPreviewComponent({ flowId }: Props) {
  const { openForm, closeAll } = useDialogStore();
  const [nodes, setNodes] = useState<TreeNodeDto[]>([]);
  const [expandedKeys, setExpandedKeys] = useState<TreeExpandedKeysType>({});
  const [loaded, setLoaded] = useState<TreeNodeDto[] | undefined>(undefined);

  // Not destructured with a default: `data = []` builds a new array every render while the
  // query has nothing, and the identity check below would then never settle.
  // The sub-flow's root steps, not Flow.getTreeNodes: that returns the flow node itself, which
  // the real tree uses as a wrapper to expand. A preview wants the steps.
  const { data, isLoading } = useQuery({
    queryKey: ["flow", "rootSteps", flowId],
    queryFn: () => backendApiService.FlowStep.getTreeNodes({ id: flowId, isFlow: true }),
    enabled: flowId > 0,
  });

  // A fresh fetch replaces whatever was expanded. Adjusted during render rather than in an
  // effect so the panel never paints one frame of the previous sub-flow's tree.
  if (data !== undefined && loaded !== data) {
    setLoaded(data);
    setNodes(data);
    setExpandedKeys({});
  }

  // Expanding fetches that branch, same as the real tree. A nested SUB_FLOW node is a leaf, so
  // the preview stops there on its own and can never unfold forever.
  const handleExpand = async (node: TreeNodeDto) => {
    if (node.children.length > 0) return;

    // isFlow decides which column the id is matched against. Hardcoding it asks for steps whose
    // parent happens to share the id, which silently returns another flow's steps.
    const children = await backendApiService.FlowStep.getTreeNodes({
      id: node.entityId,
      isFlow: node.isFlow,
    });

    setNodes((previous) => withChildren(previous, node.key, children));
  };

  const handleSelect = async (node: TreeNodeDto) => {
    if (node.isFlow || node.isNew) return;

    const step = await backendApiService.FlowStep.get(node.entityId);
    const form = getFlowStepForm(step.flowStepType);
    if (!form) return;

    const StepForm = form.component;

    openForm("sub-flow-step-view", {
      headerText: step.name,
      formId: "sub-flow-step-view",
      children: (
        <StepForm
          formMode={FormMode.VIEW}
          defaultValues={new FlowStepDto(step)}
          onSubmit={() => closeAll()}
          onCancel={() => closeAll()}
          onEdit={() => closeAll()}
        />
      ),
    });
  };

  return (
    <Panel
      header="What this runs"
      toggleable
      className="mt-3"
    >
      {isLoading && (
        <LabelComponent
          text="Loading..."
          size="sm"
          color="secondary"
        />
      )}

      {!isLoading && nodes.length === 0 && (
        <LabelComponent
          text="That sub-flow has no steps yet."
          size="sm"
          color="secondary"
        />
      )}

      <ReadOnlyFlowTreeComponent
        value={nodes}
        expandedKeys={expandedKeys}
        onToggle={setExpandedKeys}
        onExpand={(node) => void handleExpand(node)}
        onSelect={(node) => void handleSelect(node)}
      />
    </Panel>
  );
}

const withChildren = (
  nodes: TreeNodeDto[],
  key: string,
  children: TreeNodeDto[],
): TreeNodeDto[] =>
  nodes.map((node) =>
    node.key === key
      ? { ...node, children }
      : { ...node, children: withChildren(node.children, key, children) },
  );
