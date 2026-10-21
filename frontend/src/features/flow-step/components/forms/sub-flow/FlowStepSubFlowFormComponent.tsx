import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect } from "react";

import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepSubFlowSchema } from "@/features/flow-step/components/forms/sub-flow/flow-step-sub-flow.zod";
import FlowStepSubFlowFormFieldsComponent from "@/features/flow-step/components/forms/sub-flow/FlowStepSubFlowFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepSubFlowFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepSubFlowSchema>>({
    resolver: zodResolver(FlowStepSubFlowSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepSubFlowSchema>) =>
    onSubmit(
      new FlowStepDto({
        ...defaultValues,
        ...data,
        subFlowId: data.subFlowId ?? undefined,
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
            <FlowStepSubFlowFormFieldsComponent isDisabled={formMode === "VIEW"} />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
