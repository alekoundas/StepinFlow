import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowCsvColumnDto } from "@/shared/models/database/flow-csv-column-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import FlowCsvColumnFormComponent from "@/features/flow-csv-column/components/forms/FlowCsvColumnFormComponent";
import { useFlowCsvColumnMutations } from "@/features/flow-csv-column/hooks/use-flow-csv-column";
import { typesInput } from "@/features/workflow/hooks/use-flow-parts";
import { WorkflowUsedByComponent } from "@/features/workflow/components/WorkflowUsedByComponent";
import type { WorkflowPartSelection } from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  flow: FlowDto;
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

const FORM_ID = "flow-csv-column-panel-form";

/** The Inputs tab's right side: the picked input's form, saved on its own. */
export function FlowCsvColumnPanelComponent({ flow, uses, selected, onSelect }: Props) {
  const {
    createFlowCsvColumnMutation,
    updateFlowCsvColumnMutation,
    deleteFlowCsvColumnMutation,
  } = useFlowCsvColumnMutations();
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
        text="Pick an input, or add one."
        color="secondary"
      />
    );

  const input =
    selected === "new"
      ? new FlowCsvColumnDto({ flowId: flow.id })
      : flow.flowCsvColumns.find((x) => x.id === selected);

  if (!input)
    return (
      <LabelComponent
        text="That input no longer exists."
        color="secondary"
      />
    );

  const usedBy = (uses ?? []).filter((x) => typesInput(x, input.name));

  const save = async (dto: FlowCsvColumnDto) => {
    setError(undefined);
    try {
      if (formMode === FormMode.ADD) {
        const id = await createFlowCsvColumnMutation.mutateAsync(dto);
        onSelect(id);
        return;
      }

      await updateFlowCsvColumnMutation.mutateAsync(dto);
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

  // The steps keep {{name}} in their text, and the validator then says each one types a name
  // nothing has.
  const remove = () => {
    const message =
      usedBy.length > 0
        ? `${usedBy.length} step(s) type {{${input.name}}} and will have nothing to fill it with.`
        : `No step types "${input.name}".`;

    openConfirm("flow-csv-column-delete", {
      headerText: `Delete ${input.name}?`,
      confirmLabel: "Delete the input",
      confirmSeverity: "danger",
      children: <LabelComponent text={`${message} This cannot be undone.`} />,
      onConfirm: async () => {
        await deleteFlowCsvColumnMutation.mutateAsync(input.id);
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
            label="Delete input"
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

      <FlowCsvColumnFormComponent
        key={`${selected}-${resetCount}`}
        formId={FORM_ID}
        formMode={formMode}
        defaultValues={input}
        onSubmit={save}
        onCancel={cancel}
        onEdit={() => setFormMode(FormMode.EDIT)}
      />

      {selected !== "new" && (
        <WorkflowUsedByComponent
          steps={usedBy}
          emptyText="No step types it yet."
        />
      )}
    </div>
  );
}
