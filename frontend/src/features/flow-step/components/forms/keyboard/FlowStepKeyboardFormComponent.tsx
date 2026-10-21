import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepKeyboardSchema } from "@/features/flow-step/components/forms/keyboard/flow-step-keyboard.zod";
import FlowStepKeyboardFormFieldsComponent from "@/features/flow-step/components/forms/keyboard/FlowStepKeyboardFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepKeyboardFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepKeyboardSchema>>({
    resolver: zodResolver(FlowStepKeyboardSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepKeyboardSchema>) =>
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
            <FlowStepKeyboardFormFieldsComponent isDisabled={formMode === "VIEW"} />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
