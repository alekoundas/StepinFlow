import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect } from "react";

import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepCheckValueSchema } from "@/features/flow-step/components/forms/check-value/flow-step-check-value.zod";
import FlowStepCheckValueFormFieldsComponent from "@/features/flow-step/components/forms/check-value/FlowStepCheckValueFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepCheckValueFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepCheckValueSchema>>({
    resolver: zodResolver(FlowStepCheckValueSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepCheckValueSchema>) =>
    onSubmit(
      new FlowStepDto({
        ...defaultValues,
        ...data,
        flowStepReferenceId: data.flowStepReferenceId ?? undefined,
      }),
    );

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
            <FlowStepCheckValueFormFieldsComponent
              parentFlowStepId={defaultValues.parentFlowStepId}
              isDisabled={formMode === "VIEW"}
            />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
