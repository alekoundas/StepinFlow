import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormFieldsComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/components/forms/wait/flow-step-wait.zod";
import { Button } from "primereact/button";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";

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
      <FormHeaderComponent
        title="Wait Step Configuration"
        description="Pause execution for a specified duration before continuing to the next step."
        formMode={formMode}
        onEdit={onEdit}
      />
      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }),
          )}
          className="flex flex-column h-full"
        >
          <FlowStepWaitFormFieldsComponent isDisabled={formMode === "VIEW"} />

          <FormFooterComponent
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
