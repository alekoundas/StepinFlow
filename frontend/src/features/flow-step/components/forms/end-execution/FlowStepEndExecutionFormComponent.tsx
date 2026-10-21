import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepEndExecutionSchema } from "@/features/flow-step/components/forms/end-execution/flow-step-end-execution.zod";
import FlowStepEndExecutionFormFieldsComponent from "@/features/flow-step/components/forms/end-execution/FlowStepEndExecutionFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepEndExecutionFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepEndExecutionSchema>>({
    resolver: zodResolver(FlowStepEndExecutionSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const handleSubmit = (data: z.infer<typeof FlowStepEndExecutionSchema>) =>
    onSubmit(new FlowStepDto({ ...defaultValues, ...data }));

  return (
    <>
      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepFormLayoutComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onEdit={onEdit}
            onCancel={onCancel}
            baseValues={defaultValues}
          >
            <FlowStepEndExecutionFormFieldsComponent isDisabled={formMode === "VIEW"} />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
