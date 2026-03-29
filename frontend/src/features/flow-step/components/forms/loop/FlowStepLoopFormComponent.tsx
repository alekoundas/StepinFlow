import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FormMode } from "@/shared/enums/form-mode-enum";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepLoopFormFieldsComponent from "@/features/flow-step/components/forms/loop/FlowStepLoopFormFieldsComponent";
import { FormFooterActionsComponent } from "@/shared/components/form/FormFooterActionsComponent";
import { FlowStepLoopSchema } from "@/features/flow-step/schema/flow-step-loop.zod";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
}

export default function FlowStepLoopFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepLoopSchema>>({
    resolver: zodResolver(FlowStepLoopSchema),
    mode: "onChange",
    defaultValues: defaultValues,
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
        <FlowStepLoopFormFieldsComponent isDisabled={formMode === "VIEW"} />

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
