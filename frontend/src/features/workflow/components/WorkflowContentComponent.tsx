import { useEffect, useState, type ReactNode } from "react";

import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { FlowStepTypesDataGridComponent } from "@/features/flow-step/components/FlowStepTypesDataGridComponent";
import { FormMode } from "@/shared/enums/form-mode-enum";
import {
  useFlowStep,
  useFlowStepMutations,
} from "@/features/flow-step/hooks/use-flow-step";

import LabelComponent from "@/shared/components/LabelComponent";
import { useFlow, useFlowMutations } from "@/features/flow/hooks/use-flow";
import { FlowFormComponent } from "@/features/flow/components/form/FlowFormComponent";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import FlowStepWaitFormComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormComponent";
import FlowStepLoopFormComponent from "@/features/flow-step/components/forms/loop/FlowStepLoopFormComponent";
import FlowStepCursorClickFormComponent from "@/features/flow-step/components/forms/cusror-click/FlowStepCursorClickFormComponent";
import FlowStepCursorDragFormComponent from "@/features/flow-step/components/forms/cusror-drag/FlowStepCursorDragFormComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";

// interface Props {
// treeNodeDto: TreeNodeDto;
// loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
// itemTemplate: (item: T) => ReactNode;
// }

export function WorkflowContentComponent() {
  const {
    selectedTreeNode,
    selectedFlowStepTypeToAdd,
    rootFlowId,
    setTreeRefreshTrigger,
    setSelectedFlowStepTypeToAdd,
  } = useWorkflowStore();

  const [formMode, setFormMode] = useState<FormMode>(FormMode.VIEW);

  useEffect(() => {
    if (selectedTreeNode && selectedTreeNode.isNew) {
      setFormMode(FormMode.ADD);
    } else {
      setFormMode(FormMode.VIEW);
    }
  }, [selectedTreeNode]);

  // ── React Query for Flow (when root node is selected) ──
  const flowId = selectedTreeNode?.isFlow ? +selectedTreeNode.key : null;
  const { data: loadedFlow, isLoading: flowLoading } = useFlow(flowId);

  // ── React Query for FlowStep ──
  const stepId =
    selectedTreeNode && !selectedTreeNode.isNew && !selectedTreeNode.isFlow
      ? +selectedTreeNode.key
      : null;
  const { data: loadedStep, isLoading: stepLoading } = useFlowStep(stepId);
  const { createFlowStepMutation, updateFlowStepMutation } =
    useFlowStepMutations();
  const { updateFlowMutation } = useFlowMutations();

  const handleFlowSave = async (saveDto: FlowDto) => {
    await updateFlowMutation.mutateAsync(saveDto);

    setTreeRefreshTrigger({
      id: -1,
      isFlow: true,
    });

    setFormMode(FormMode.VIEW);
  };
  const handleSave = async (saveDto: FlowStepDto) => {
    if (formMode === FormMode.ADD) {
      const result = await createFlowStepMutation.mutateAsync(saveDto);

      if (saveDto.parentFlowStepId) {
        setTreeRefreshTrigger({
          id: saveDto.parentFlowStepId,
          isFlow: false,
          selectNodeIdAfterLoad: result,
        });
      }
      if (saveDto.flowId) {
        setTreeRefreshTrigger({
          id: saveDto.flowId,
          isFlow: true,
          selectNodeIdAfterLoad: result,
        });
      }

      setSelectedFlowStepTypeToAdd(undefined);
    } else if (formMode === FormMode.EDIT) {
      await updateFlowStepMutation.mutateAsync(saveDto);

      if (saveDto.parentFlowStepId) {
        setTreeRefreshTrigger({
          id: saveDto.parentFlowStepId,
          isFlow: false,
        });
      }
      if (saveDto.flowId) {
        setTreeRefreshTrigger({
          id: saveDto.flowId,
          isFlow: true,
        });
      }
    }

    setFormMode(FormMode.VIEW);
  };

  // ====================== RENDER ======================
  if (!selectedTreeNode) {
    return (
      <div className=" ">
        <LabelComponent
          text="Select a node from the tree"
          size="lg"
        />
      </div>
    );
  }

  // 1. New FlowStep → type picker
  if (selectedTreeNode.isNew && !selectedFlowStepTypeToAdd) {
    return (
      <div className=" ">
        <FlowStepTypesDataGridComponent />
      </div>
    );
  }

  // 2. New FlowStep → ADD form
  if (selectedTreeNode.isNew && selectedFlowStepTypeToAdd) {
    let formElement: ReactNode;
    switch (selectedFlowStepTypeToAdd) {
      case FlowStepTypeEnum.WAIT:
        formElement = (
          <FlowStepWaitFormComponent
            formMode={formMode}
            onSubmit={handleSave}
            onCancel={() => setSelectedFlowStepTypeToAdd(undefined)}
            onEdit={() => {}}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                orderNumber: selectedTreeNode.orderNumber,
                rootId: rootFlowId,
                flowStepType: "WAIT",
                name: "Wait",
                waitForMilliseconds: 50,
              })
            }
          />
        );
        break;

      case FlowStepTypeEnum.LOOP:
        formElement = (
          <FlowStepLoopFormComponent
            formMode={formMode}
            onSubmit={handleSave}
            onCancel={() => setSelectedFlowStepTypeToAdd(undefined)}
            onEdit={() => {}}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                orderNumber: selectedTreeNode.orderNumber,
                rootId: rootFlowId,
                flowStepType: "LOOP",
                name: "Loop",
              })
            }
          />
        );
        break;

      case FlowStepTypeEnum.CURSOR_CLICK:
        formElement = (
          <FlowStepCursorClickFormComponent
            formMode={formMode}
            onSubmit={handleSave}
            onCancel={() => setSelectedFlowStepTypeToAdd(undefined)}
            onEdit={() => {}}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                orderNumber: selectedTreeNode.orderNumber,
                rootId: rootFlowId,
                flowStepType: "CURSOR_CLICK",
                name: "Cursor Click",
              })
            }
          />
        );
        break;

      case FlowStepTypeEnum.CURSOR_DRAG:
        formElement = (
          <FlowStepCursorDragFormComponent
            formMode={formMode}
            onSubmit={handleSave}
            onCancel={() => setSelectedFlowStepTypeToAdd(undefined)}
            onEdit={() => {}}
            defaultValues={
              new FlowStepDto({
                flowId: selectedTreeNode.parentFlowId,
                parentFlowStepId: selectedTreeNode.parentFlowStepId,
                orderNumber: selectedTreeNode.orderNumber,
                rootId: rootFlowId,
                flowStepType: "CURSOR_DRAG",
                name: "Cursor Drag & Drop",
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
    return <div className=" ">{formElement}</div>;
  }

  // 3. Flow node (root) → Flow form
  if (selectedTreeNode.isFlow) {
    if (flowLoading) {
      return (
        <div className=" ">
          <LabelComponent text="Loading flow..." />
        </div>
      );
    }
    if (!loadedFlow) {
      return (
        <div className=" ">
          <LabelComponent
            text="Failed to load flow"
            className="p-error"
          />
        </div>
      );
    }

    return (
      <div className=" ">
        <FlowFormComponent
          formMode={formMode}
          defaultValues={loadedFlow}
          onSubmit={handleFlowSave}
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
        />
      </div>
    );
  }

  // 4. Existing FlowStep → VIEW
  if (stepLoading) {
    return (
      <div className=" ">
        <LabelComponent text="Loading flow step..." />
      </div>
    );
  }
  if (!loadedStep) {
    return (
      <div className=" ">
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
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
          defaultValues={new FlowStepDto(flowStepDto)}
        />
      );
      break;
    case FlowStepTypeEnum.LOOP:
      formElement = (
        <FlowStepLoopFormComponent
          formMode={formMode}
          onSubmit={handleSave}
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
          defaultValues={new FlowStepDto(flowStepDto)}
        />
      );
      break;

    case FlowStepTypeEnum.CURSOR_CLICK:
      formElement = (
        <FlowStepCursorClickFormComponent
          formMode={formMode}
          onSubmit={handleSave}
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
          defaultValues={new FlowStepDto(flowStepDto)}
        />
      );
      break;

    case FlowStepTypeEnum.CURSOR_DRAG:
      formElement = (
        <FlowStepCursorDragFormComponent
          formMode={formMode}
          onSubmit={handleSave}
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
          defaultValues={new FlowStepDto(flowStepDto)}
        />
      );
      break;

    default:
      formElement = <LabelComponent text="Unsupported flow step type" />;
  }

  return <div className=" ">{formElement}</div>;
}
