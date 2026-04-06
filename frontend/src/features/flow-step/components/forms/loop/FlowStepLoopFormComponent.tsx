import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FormMode } from "@/shared/enums/form-mode-enum";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import FlowStepLoopFormFieldsComponent from "@/features/flow-step/components/forms/loop/FlowStepLoopFormFieldsComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepLoopSchema } from "@/features/flow-step/components/forms/loop/flow-step-loop.zod";
import { useEffect } from "react";
import { Button } from "primereact/button";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";

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
    <div>
      <div className="flex justify-content-between">
        <div>
          <LabelComponent
            text="Loop Step Configuration"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text="Repeat a set of child steps a specified number of times or for ever."
            size="xs"
            className="mt-1"
          />
        </div>
        <Button
          icon="pi pi-pencil"
          label="Edit"
          className="p-button-outlined p-button-secondary"
          visible={formMode === "VIEW"}
          onClick={onEdit}
        />
      </div>
      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }),
          )}
          className="flex flex-column h-full"
        >
          <FlowStepLoopFormFieldsComponent
            isDisabled={formMode === "VIEW"}
            className="mt-5 ml-3 mr-4"
          />

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </div>
  );
}
