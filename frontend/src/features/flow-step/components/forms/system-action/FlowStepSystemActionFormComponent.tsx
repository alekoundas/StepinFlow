import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepSystemActionSchema } from "@/features/flow-step/components/forms/system-action/flow-step-system-action.zod";
import FlowStepSystemActionFormFieldsComponent from "@/features/flow-step/components/forms/system-action/FlowStepSystemActionFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepSystemActionFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepSystemActionSchema>>({
    resolver: zodResolver(FlowStepSystemActionSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  const handleSubmit = (data: z.infer<typeof FlowStepSystemActionSchema>) =>
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
            <FlowStepSystemActionFormFieldsComponent isDisabled={formMode === "VIEW"} />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
