import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import FlowPointFormComponent from "@/features/flow-point/components/forms/FlowPointFormComponent";
import { useFlowPointMutations } from "@/features/flow-point/hooks/use-flow-point";
import { WorkflowUsedByComponent } from "@/features/workflow/components/WorkflowUsedByComponent";
import type { WorkflowPartSelection } from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  flow: FlowDto;
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

const FORM_ID = "flow-point-panel-form";

/** The Locations tab's right side: the picked location's form, saved on its own. */
export function FlowPointPanelComponent({ flow, uses, selected, onSelect }: Props) {
  const { createFlowPointMutation, updateFlowPointMutation, deleteFlowPointMutation } =
    useFlowPointMutations();
  const { openConfirm } = useDialogStore();

  const [formMode, setFormMode] = useState<FormMode>(FormMode.VIEW);
  const [modeKey, setModeKey] = useState<WorkflowPartSelection>(selected);
  const [resetCount, setResetCount] = useState(0);
  const [error, setError] = useState<string>();

  if (modeKey !== selected) {
    setModeKey(selected);
    setFormMode(selected === "new" ? FormMode.ADD : FormMode.VIEW);
    setError(undefined);
  }

  if (selected == null)
    return (
      <LabelComponent
        text="Pick a location, or add one."
        color="secondary"
      />
    );

  const point =
    selected === "new"
      ? new FlowPointDto({ flowId: flow.id })
      : flow.flowPoints.find((x) => x.id === selected);

  if (!point)
    return (
      <LabelComponent
        text="That location no longer exists."
        color="secondary"
      />
    );

  const usedBy = (uses ?? []).filter(
    (x) => x.flowPointId === point.id || x.flowPointEndId === point.id,
  );

  const save = async (dto: FlowPointDto) => {
    setError(undefined);
    try {
      if (formMode === FormMode.ADD) {
        const id = await createFlowPointMutation.mutateAsync(dto);
        onSelect(id);
        return;
      }

      await updateFlowPointMutation.mutateAsync(dto);
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
    const message =
      usedBy.length > 0
        ? `"${point.name}" is used by ${usedBy.length} step(s). Deleting it clears their location.`
        : `Nothing uses "${point.name}".`;

    openConfirm("flow-point-delete", {
      headerText: `Delete ${point.name}?`,
      confirmLabel: "Delete the location",
      confirmSeverity: "danger",
      children: <LabelComponent text={`${message} This cannot be undone.`} />,
      onConfirm: async () => {
        await deleteFlowPointMutation.mutateAsync(point.id);
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
            label="Delete location"
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

      <FlowPointFormComponent
        key={`${selected}-${resetCount}`}
        formId={FORM_ID}
        formMode={formMode}
        defaultValues={point}
        areaOptions={flow.flowAreas}
        onSubmit={save}
        onCancel={cancel}
        onEdit={() => setFormMode(FormMode.EDIT)}
      />

      {selected !== "new" && (
        <WorkflowUsedByComponent
          steps={usedBy}
          emptyText="No step moves to it yet."
        />
      )}
    </div>
  );
}
