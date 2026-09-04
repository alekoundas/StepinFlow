import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

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
import { getFlowStepForm } from "@/features/flow-step/components/forms/flow-step-form-registry";
import { useWizardStore } from "@/features/wizard/store/wizard-store";
import ExtractSubFlowButtonComponent from "@/features/workflow/components/ExtractSubFlowButtonComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import { useDeleteFlow } from "@/features/flow/hooks/use-delete-flow";
import { usePromoteFlow } from "@/features/flow/hooks/use-promote-flow";
import { useDeleteFlowStep } from "@/features/flow-step/hooks/use-delete-flow-step";

export function WorkflowContentComponent() {
  const {
    selectedTreeNode,
    selectedFlowStepTypeToAdd,
    rootFlowId,
    setTreeRefreshTrigger,
    setSelectedFlowStepTypeToAdd,
    setSelectedTreeNode,
  } = useWorkflowStore();

  const navigate = useNavigate();
  const { setTarget } = useWizardStore();

  const [formMode, setFormMode] = useState<FormMode>(FormMode.VIEW);
  const [modeNodeKey, setModeNodeKey] = useState<string | undefined>(undefined);

  // Selecting a different row resets the mode: a placeholder row opens in ADD, a saved one in
  // VIEW. Adjusted during render rather than in an effect, so the form never paints one frame
  // with the mode of the row that was selected before it.
  if (modeNodeKey !== selectedTreeNode?.key) {
    setModeNodeKey(selectedTreeNode?.key);
    setFormMode(selectedTreeNode?.isNew ? FormMode.ADD : FormMode.VIEW);
  }

  // ── React Query for Flow (when root node is selected) ──
  const flowId = selectedTreeNode?.isFlow ? selectedTreeNode.entityId : null;
  const { data: loadedFlow, isLoading: flowLoading } = useFlow(flowId);

  // ── React Query for FlowStep ──
  const stepId =
    selectedTreeNode && !selectedTreeNode.isNew && !selectedTreeNode.isFlow
      ? selectedTreeNode.entityId
      : null;
  const { data: loadedStep, isLoading: stepLoading } = useFlowStep(stepId);
  const { createFlowStepMutation, updateFlowStepMutation } =
    useFlowStepMutations();
  const { updateFlowMutation } = useFlowMutations();

  const deleteFlow = useDeleteFlow();
  const promoteFlow = usePromoteFlow();
  const deleteFlowStep = useDeleteFlowStep();

  // The deleted step is the one on screen, so the tree has to reload and the selection has to let
  // go of a node that no longer exists.
  const handleStepDeleted = () => {
    const node = selectedTreeNode;

    setSelectedTreeNode(undefined);

    if (node?.parentFlowStepId)
      setTreeRefreshTrigger({ id: node.parentFlowStepId, isFlow: false });

    if (node?.parentFlowId)
      setTreeRefreshTrigger({ id: node.parentFlowId, isFlow: true });
  };

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

  const panel = (children: React.ReactNode) => <div className=" ">{children}</div>;

  if (!selectedTreeNode) {
    return panel(
      <LabelComponent
        text="Select a node from the tree"
        size="lg"
      />,
    );
  }

  // 1. New FlowStep → type picker
  if (selectedTreeNode.isNew && !selectedFlowStepTypeToAdd) {
    // Recording lands the steps exactly where this placeholder sits, so the position the user
    // already chose in the tree is the position they get.
    const startRecording = () => {
      setTarget(
        {
          targetFlowId: selectedTreeNode.parentFlowStepId
            ? undefined
            : selectedTreeNode.parentFlowId,
          targetParentFlowStepId: selectedTreeNode.parentFlowStepId ?? undefined,
          targetIndex: selectedTreeNode.orderNumber,
        },
        rootFlowId,
      );
      navigate("/record");
    };

    return panel(
      <>
        <div className="flex justify-content-end mb-3">
          <Button
            type="button"
            label="Record steps here"
            icon="pi pi-circle-fill"
            onClick={startRecording}
            className="p-button-outlined"
            tooltip="Do the task once and let the steps be written into this position"
            tooltipOptions={{ position: "left" }}
          />
        </div>
        <FlowStepTypesDataGridComponent />
      </>,
    );
  }

  // 2. New FlowStep → ADD form
  if (selectedTreeNode.isNew && selectedFlowStepTypeToAdd) {
    const form = getFlowStepForm(selectedFlowStepTypeToAdd);

    if (!form) {
      return panel(
        <LabelComponent
          text={`Form for type ${selectedFlowStepTypeToAdd} not implemented yet`}
        />,
      );
    }

    const StepForm = form.component;

    return panel(
      <StepForm
        formMode={formMode}
        onSubmit={handleSave}
        onCancel={() => setSelectedFlowStepTypeToAdd(undefined)}
        onEdit={() => {}}
        defaultValues={
          new FlowStepDto({
            flowId: selectedTreeNode.parentFlowId ?? undefined,
            parentFlowStepId: selectedTreeNode.parentFlowStepId ?? undefined,
            orderNumber: selectedTreeNode.orderNumber,
            rootId: rootFlowId,
            flowStepType: selectedFlowStepTypeToAdd,
            ...form.newStepValues(selectedFlowStepTypeToAdd),
          })
        }
      />,
    );
  }

  // 3. Flow node (root) → Flow form
  if (selectedTreeNode.isFlow) {
    if (flowLoading) return panel(<LabelComponent text="Loading flow..." />);

    if (!loadedFlow) {
      return panel(
        <LabelComponent
          text="Failed to load flow"
          className="p-error"
        />,
      );
    }

    return panel(
      <>
        {/* Only in VIEW, so it is never one button away from Save. */}
        {formMode === FormMode.VIEW && (
          <div className="flex justify-content-end gap-2 mb-2">
            {/* Only offered on a flow: a sub-flow is already one, and it is a one-way trip. */}
            {!loadedFlow.isSubFlow && (
              <Button
                type="button"
                label="Make this a sub-flow"
                icon="pi pi-sitemap"
                outlined
                onClick={() => promoteFlow(loadedFlow.id)}
              />
            )}

            <Button
              type="button"
              label="Delete flow"
              icon="pi pi-trash"
              outlined
              severity="danger"
              onClick={() =>
                deleteFlow(loadedFlow, () => navigate("/flows"))
              }
            />
          </div>
        )}

        <FlowFormComponent
          key={flowId}
          formMode={formMode}
          defaultValues={loadedFlow}
          onSubmit={handleFlowSave}
          onCancel={() => setFormMode(FormMode.VIEW)}
          onEdit={() => setFormMode(FormMode.EDIT)}
        />
      </>,
    );
  }

  // 4. Existing FlowStep → VIEW / EDIT
  if (stepLoading) return panel(<LabelComponent text="Loading flow step..." />);

  if (!loadedStep) {
    return panel(
      <LabelComponent
        text="Failed to load step"
        className="p-error"
      />,
    );
  }

  const form = getFlowStepForm(selectedTreeNode.flowStepType);
  if (!form) return panel(<LabelComponent text="Unsupported flow step type" />);

  const StepForm = form.component;

  // Structural branches belong to the step above them, so there is nothing there to lift out -
  // and nothing to delete on its own either. A Failure branch goes when its step goes.
  const isStructural =
    selectedTreeNode.flowStepType === FlowStepTypeEnum.SUCCESS ||
    selectedTreeNode.flowStepType === FlowStepTypeEnum.FAILURE;

  // react-hook-form reads defaultValues once, on mount. Two steps of the same type render the
  // same component, so without a key React reuses the instance and the form keeps the first
  // step's values. The key also drops any unsaved edits with the step they belonged to.
  return panel(
    <>
      {!isStructural && formMode === FormMode.VIEW && (
        <div className="flex justify-content-end gap-2 mb-2">
          <ExtractSubFlowButtonComponent
            node={selectedTreeNode}
            rootFlowId={rootFlowId ?? 0}
            onExtracted={({ flowStepId, parentId, isFlow }) =>
              setTreeRefreshTrigger({
                id: parentId,
                isFlow,
                selectNodeIdAfterLoad: flowStepId,
              })
            }
          />

          <Button
            type="button"
            label="Delete step"
            icon="pi pi-trash"
            outlined
            severity="danger"
            onClick={() => deleteFlowStep(selectedTreeNode, handleStepDeleted)}
          />
        </div>
      )}

      <StepForm
        key={stepId}
        formMode={formMode}
        onSubmit={handleSave}
        onCancel={() => setFormMode(FormMode.VIEW)}
        onEdit={() => setFormMode(FormMode.EDIT)}
        defaultValues={new FlowStepDto(loadedStep as FlowStepDto)}
      />
    </>,
  );
}
