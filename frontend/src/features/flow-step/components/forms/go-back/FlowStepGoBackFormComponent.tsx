import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FlowStepGoBackSchema } from "@/features/flow-step/components/forms/go-back/flow-step-go-back.zod";
import FlowStepGoBackFormFieldsComponent from "@/features/flow-step/components/forms/go-back/FlowStepGoBackFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepGoBackFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepGoBackSchema>>({
    resolver: zodResolver(FlowStepGoBackSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepGoBackSchema>) =>
    onSubmit(
      new FlowStepDto({
        ...defaultValues,
        ...data,
        flowStepReferenceId: data.flowStepReferenceId ?? undefined,
      }),
    );

  return (
    <>
      <FormHeaderComponent
        title="Go Back Step Configuration"
        description="Return to a step this one already passed on the way here, and carry on from there."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepGoBackFormFieldsComponent
            rootId={defaultValues.rootId}
            parentFlowStepId={defaultValues.parentFlowStepId}
            orderNumber={defaultValues.orderNumber}
            isDisabled={formMode === "VIEW"}
          />

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
