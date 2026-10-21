import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormFieldsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/components/forms/wait/flow-step-wait.zod";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";

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
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
          onSubmit({ ...defaultValues, ...partialDto }),
        )}
      >
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
        >
          <FlowStepWaitFormFieldsComponent isDisabled={formMode === "VIEW"} />
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
