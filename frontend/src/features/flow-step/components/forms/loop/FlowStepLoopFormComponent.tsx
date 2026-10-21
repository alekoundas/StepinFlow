import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FormMode } from "@/shared/enums/form-mode-enum";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepLoopFormFieldsComponent from "@/features/flow-step/components/forms/loop/FlowStepLoopFormFieldsComponent";
import { FlowStepLoopSchema } from "@/features/flow-step/components/forms/loop/flow-step-loop.zod";
import { useEffect } from "react";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepLoopFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepLoopSchema>>({
    resolver: zodResolver(FlowStepLoopSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  // Force initial validation (both fields)
  useEffect(() => {
    const timer = setTimeout(() => {
      trigger(["loopCount", "isLoopInfinite"]);
    }, 0);

    return () => clearTimeout(timer);
  }, [trigger]);

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
          <FlowStepLoopFormFieldsComponent isDisabled={formMode === "VIEW"} />
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
