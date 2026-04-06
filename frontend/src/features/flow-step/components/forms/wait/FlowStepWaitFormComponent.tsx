import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormFieldsComponent";
import { FormFooterActionsComponent } from "@/shared/components/form/FormFooterActionsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/components/forms/wait/flow-step-wait.zod";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepWaitFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepWaitSchema>>({
    resolver: zodResolver(FlowStepWaitSchema),
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
            text="Wait Step Configuration"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text="Pause execution for a specified duration before continuing to the next step."
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
          <FlowStepWaitFormFieldsComponent isDisabled={formMode === "VIEW"} />

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
