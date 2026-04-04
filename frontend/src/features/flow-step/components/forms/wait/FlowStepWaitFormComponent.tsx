import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormFieldsComponent";
import { FormFooterActionsComponent } from "@/shared/components/form/FormFooterActionsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/schema/flow-step-wait.zod";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
}

export default function FlowStepWaitFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
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
  );
}
