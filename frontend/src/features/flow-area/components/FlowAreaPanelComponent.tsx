import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import FlowAreaFormComponent from "@/features/flow-area/components/forms/FlowAreaFormComponent";
import { useFlowAreaMutations } from "@/features/flow-area/hooks/use-flow-area";
import { WorkflowUsedByComponent } from "@/features/workflow/components/WorkflowUsedByComponent";
import type { WorkflowPartSelection } from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  flow: FlowDto;
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

const FORM_ID = "flow-area-panel-form";

/** The Areas tab's right side: the picked area's form, saved on its own. */
export function FlowAreaPanelComponent({ flow, uses, selected, onSelect }: Props) {
  const { createFlowAreaMutation, updateFlowAreaMutation, deleteFlowAreaMutation } =
    useFlowAreaMutations();
  const { openConfirm } = useDialogStore();

  const [formMode, setFormMode] = useState<FormMode>(FormMode.VIEW);
  const [modeKey, setModeKey] = useState<WorkflowPartSelection>(selected);
  const [resetCount, setResetCount] = useState(0);
  const [error, setError] = useState<string>();

  // A new pick opens in VIEW, or in ADD for a new one. Adjusted during render, so the form never
  // paints once in the mode of the area picked before.
  if (modeKey !== selected) {
    setModeKey(selected);
    setFormMode(selected === "new" ? FormMode.ADD : FormMode.VIEW);
    setError(undefined);
  }

  if (selected == null)
    return (
      <LabelComponent
        text="Pick an area, or add one."
        color="secondary"
      />
    );

  const area =
    selected === "new"
      ? new FlowAreaDto({ flowId: flow.id })
      : flow.flowAreas.find((x) => x.id === selected);

  if (!area)
    return (
      <LabelComponent
        text="That area no longer exists."
        color="secondary"
      />
    );

  // Areas go one level deep, so only one with nothing around it may hold this one.
  const parentOptions = flow.flowAreas.filter((x) => !x.parentFlowAreaId && x.id !== area.id);
  const childAreas = flow.flowAreas.filter((x) => x.parentFlowAreaId === area.id);
  const usedBy = (uses ?? []).filter((x) => x.flowAreaId === area.id);

  const save = async (dto: FlowAreaDto) => {
    setError(undefined);
    try {
      if (formMode === FormMode.ADD) {
        const id = await createFlowAreaMutation.mutateAsync(dto);
        onSelect(id);
        return;
      }

      await updateFlowAreaMutation.mutateAsync(dto);
      // From what was saved, so putting a field back afterwards counts as a change.
      setResetCount((x) => x + 1);
      setFormMode(FormMode.VIEW);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const cancel = () => {
    if (selected === "new") {
      onSelect(null);
      return;
    }

    setResetCount((x) => x + 1);
    setFormMode(FormMode.VIEW);
  };

  const remove = () => {
    const regions = childAreas.length > 0 ? ` and holds ${childAreas.length} region(s)` : "";
    const message =
      usedBy.length > 0 || childAreas.length > 0
        ? `"${area.name}" is used by ${usedBy.length} step(s)${regions}. Deleting it clears those references.`
        : `Nothing uses "${area.name}".`;

    openConfirm("flow-area-delete", {
      headerText: `Delete ${area.name}?`,
      confirmLabel: "Delete the area",
      confirmSeverity: "danger",
      children: <LabelComponent text={`${message} This cannot be undone.`} />,
      onConfirm: async () => {
        await deleteFlowAreaMutation.mutateAsync(area.id);
        onSelect(null);
      },
    });
  };

  return (
    <div className="flex flex-column">
      {formMode === FormMode.VIEW && (
        <div className="flex justify-content-end mb-2">
          <Button
            type="button"
            label="Delete area"
            icon="pi pi-trash"
            outlined
            severity="danger"
            onClick={remove}
          />
        </div>
      )}

      {error && (
        <Message
          severity="error"
          text={error}
          className="w-full justify-content-start mb-3"
        />
      )}

      <FlowAreaFormComponent
        key={`${selected}-${resetCount}`}
        formId={FORM_ID}
        formMode={formMode}
        defaultValues={area}
        parentOptions={parentOptions}
        childAreas={childAreas}
        onSubmit={save}
        onCancel={cancel}
        onEdit={() => setFormMode(FormMode.EDIT)}
      />

      {selected !== "new" && (
        <WorkflowUsedByComponent
          steps={usedBy}
          emptyText="No step searches in it yet."
        />
      )}
    </div>
  );
}
