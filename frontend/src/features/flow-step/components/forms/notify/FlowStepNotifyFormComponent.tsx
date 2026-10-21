import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepNotifySchema } from "@/features/flow-step/components/forms/notify/flow-step-notify.zod";
import FlowStepNotifyFormFieldsComponent from "@/features/flow-step/components/forms/notify/FlowStepNotifyFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepNotifyFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepNotifySchema>>({
    resolver: zodResolver(FlowStepNotifySchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepNotifySchema>) =>
    onSubmit(
      new FlowStepDto({
        ...defaultValues,
        ...data,
        discordBotId: data.discordBotId ?? undefined,
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
            <FlowStepNotifyFormFieldsComponent
              parentFlowStepId={defaultValues.parentFlowStepId}
              isDisabled={formMode === "VIEW"}
            />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
