import { type ReactNode } from "react";

import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { FlowStepTypesDataGridComponent } from "@/features/flow-step/components/FlowStepTypesDataGridComponent";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { FormMode } from "@/shared/enums/form-mode-enum";
import {
  useFlowStep,
  useFlowStepMutations,
} from "@/features/flow-step/hooks/use-flow-step";

import LabelComponent from "@/shared/components/LabelComponent";
import FlowStepWaitFormComponent from "@/features/flow-step/components/forms/FlowStepWaitFormComponent";
import { useFlow } from "@/features/flow/hooks/use-flow";
import { FlowFormComponent } from "@/features/flow/components/form/FlowFormComponent";
import { FlowDto } from "@/shared/models/database/flow/flow-dto";

// interface Props {
// treeNodeDto: TreeNodeDto;
// loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
// itemTemplate: (item: T) => ReactNode;
// }

export function WorkflowContentComponent() {
  const {
    selectedTreeNode,
    selectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
    setSelectedFlowStepTypeToAdd,
  } = useWorkflowStore();

  const formMode: FormMode = selectedFlowStepTypeToAdd
    ? FormMode.ADD
    : FormMode.VIEW;

  // ── React Query for Flow (when root node is selected) ──
  const flowId = selectedTreeNode?.isFlow ? +selectedTreeNode.key : null;
  const { data: loadedFlow, isLoading: flowLoading } = useFlow(flowId);

  // ── React Query for FlowStep (already had this) ──
  const stepId =
    selectedTreeNode && !selectedTreeNode.isNew && !selectedTreeNode.isFlow
      ? +selectedTreeNode.key
      : null;
  const { data: loadedStep, isLoading: stepLoading } = useFlowStep(stepId);

  const { createMutation } = useFlowStepMutations();

  const handleSave = async (saveDto: FlowStepDto) => {
    if (formMode === FormMode.ADD) {
      const result = await createMutation.mutateAsync(saveDto);

      if (saveDto.parentFlowStepId) {
        setTreeRefreshTrigger({
          id: saveDto.parentFlowStepId,
          isFlow: false,
          selectNodeIdAfterLoad: result.newId,
        });
      }
      if (saveDto.flowId) {
        setTreeRefreshTrigger({
          id: saveDto.flowId,
          isFlow: true,
          selectNodeIdAfterLoad: result.newId,
        });
      }

      setSelectedFlowStepTypeToAdd(undefined);
    }
  };

  // ====================== RENDER ======================
  if (!selectedTreeNode) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent
          text="Select a node from the tree"
          size="lg"
        />
      </div>
    );
  }

  // 1. New step → type picker
  if (selectedTreeNode.isNew && !selectedFlowStepTypeToAdd) {
    return (
      <div className="m-4 mr-3">
        <FlowStepTypesDataGridComponent />
      </div>
    );
  }

  // 2. New step + type chosen → ADD form
  if (selectedTreeNode.isNew && selectedFlowStepTypeToAdd) {
    let formElement: ReactNode;
    switch (selectedFlowStepTypeToAdd) {
      case FlowStepTypeEnum.WAIT:
        formElement = (
          <FlowStepWaitFormComponent
            formMode={formMode}
            onSubmit={handleSave}
            onCancel={() => {}}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                orderNumber: selectedTreeNode.orderNumber,
                flowStepType: "WAIT",
                name: "Wait",
                waitForMilliseconds: 50,
              })
            }
          />
        );
        break;
      default:
        formElement = (
          <LabelComponent
            text={`Form for type ${selectedFlowStepTypeToAdd} not implemented yet`}
          />
        );
    }
    return <div className="m-4 mr-3">{formElement}</div>;
  }

  // 3. Flow node (root) → Flow form
  if (selectedTreeNode.isFlow) {
    if (flowLoading) {
      return (
        <div className="m-4 mr-3">
          <LabelComponent text="Loading flow..." />
        </div>
      );
    }
    if (!loadedFlow) {
      return (
        <div className="m-4 mr-3">
          <LabelComponent
            text="Failed to load flow"
            className="p-error"
          />
        </div>
      );
    }

    console.log(loadedFlow);
    console.log(new FlowStepDto(loadedFlow));
    return (
      <div className="m-4 mr-3">
        <FlowFormComponent
          formMode={formMode}
          defaultValues={loadedFlow}
          onSubmit={() => {}}
          onCancel={() => {}}
        />
      </div>
    );
  }

  // 4. Existing FlowStep → step form
  if (stepLoading) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent text="Loading flow step..." />
      </div>
    );
  }
  if (!loadedStep) {
    return (
      <div className="m-4 mr-3">
        <LabelComponent
          text="Failed to load step"
          className="p-error"
        />
      </div>
    );
  }

  const flowStepDto = loadedStep as FlowStepDto;
  let formElement: ReactNode;

  switch (selectedTreeNode.flowStepType) {
    case FlowStepTypeEnum.WAIT:
      formElement = (
        <FlowStepWaitFormComponent
          formMode={formMode}
          onSubmit={handleSave}
          onCancel={() => {}}
          defaultValues={new FlowStepDto(flowStepDto)}
        />
      );
      break;
    default:
      formElement = <LabelComponent text="Unsupported flow step type" />;
  }

  return <div className="m-4 mr-3">{formElement}</div>;
}
