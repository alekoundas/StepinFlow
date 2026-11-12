import { Button } from "primereact/button";
import { Dropdown } from "primereact/dropdown";
import { useFormContext } from "react-hook-form";

import { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowInputDto } from "@/shared/models/database/flow-input-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { useFlow } from "@/features/flow/hooks/use-flow";
import FlowInputFormComponent from "@/features/flow-input/components/forms/FlowInputFormComponent";
import { useFlowInputMutations } from "@/features/flow-input/hooks/use-flow-input";

interface Props {
  flowId: number | undefined;
  isDisabled: boolean;
}

/**
 * Puts {{name}} of one of the flow's inputs at the end of the text to type, or adds a new input
 * and puts that in. The text stays free: an input is only a name written in it.
 */
export function FlowStepInputPickerComponent({ flowId, isDisabled }: Props) {
  const { getValues, setValue } = useFormContext();
  const { openForm, closeAll } = useDialogStore();
  const { data: flow } = useFlow(flowId ?? null);
  const { createFlowInputMutation } = useFlowInputMutations();

  const inputs = flow?.flowInputs ?? [];

  const insert = (name: string) => {
    const text = (getValues("keyboardInputText") as string | undefined) ?? "";
    setValue("keyboardInputText", `${text}{{${name}}}`, { shouldDirty: true, shouldValidate: true });
  };

  // A name another part of the flow has comes back numbered, so it is read from what was saved.
  const openAddInput = () => {
    if (!flowId) return;

    openForm("flow-input-form", {
      headerText: "Add Input",
      formId: "flow-input-form",
      children: (
        <FlowInputFormComponent
          defaultValues={new FlowInputDto({ flowId })}
          formId="flow-input-form"
          isFormInDialog={true}
          formMode={FormMode.ADD}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const id = await createFlowInputMutation.mutateAsync({ ...data, flowId });
            const saved = await backendApiService.Flow.get(flowId);
            const input = saved.flowInputs.find((x) => x.id === id);
            if (input) insert(input.name);
          }}
        />
      ),
    });
  };

  return (
    <div className="flex gap-2 align-items-center mb-3">
      <Dropdown
        value={null}
        options={inputs.map((x) => ({ label: x.name, value: x.name }))}
        onChange={(e) => insert(e.value as string)}
        placeholder={inputs.length > 0 ? "Insert an input" : "No inputs yet"}
        disabled={isDisabled || inputs.length === 0}
        className="flex-1"
      />

      <Button
        type="button"
        icon="pi pi-plus"
        label="New input"
        outlined
        onClick={openAddInput}
        disabled={isDisabled || !flowId}
      />
    </div>
  );
}
