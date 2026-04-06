import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterActionsComponent } from "@/shared/components/form/FormFooterActionsComponent";
import FlowStepCursorClickFormFieldsComponent from "@/features/flow-step/components/forms/cusror-click/FlowStepCursorClickFormFieldsComponent";
import { FlowStepCursorClickSchema } from "@/features/flow-step/components/forms/cusror-click/flow-step-cursor-click.zod";
import { Button } from "primereact/button";
import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepCursorClickFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepCursorClickSchema>>({
    resolver: zodResolver(FlowStepCursorClickSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  return (
    <div>
      <div className="flex justify-content-between">
        <div>
          <LabelComponent
            text="Cursor Click Step Configuration"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text="Simulate a left, right, or double mouse click at the specified screen coordinates."
            size="xs"
            className="mt-1"
          />
        </div>
        <Button
          icon="pi pi-pencil"
          label="Edit"
          className="p-button-outlined p-button-secondary"
          visible={formMode === "VIEW"}
          onClick={onEdit}
        />
      </div>
      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }),
          )}
          className="flex flex-column h-full"
        >
          <FlowStepCursorClickFormFieldsComponent
            isDisabled={formMode === "VIEW"}
          />

          <FormFooterActionsComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </div>
  );
}
