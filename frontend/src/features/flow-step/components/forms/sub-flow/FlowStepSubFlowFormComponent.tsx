import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";
import { useEffect } from "react";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
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
        invokedFlowId: data.invokedFlowId ?? undefined,
      }),
    );

  return (
    <>
      <FormHeaderComponent
        title="Sub-Flow Step Configuration"
        description="Run another flow from here, then carry on with the next step."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepSubFlowFormFieldsComponent isDisabled={formMode === "VIEW"} />

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </>
  );
}
