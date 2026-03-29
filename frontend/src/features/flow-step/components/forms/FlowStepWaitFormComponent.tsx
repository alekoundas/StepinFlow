import type { FormMode } from "@/shared/enums/form-mode-enum";
import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/FlowStepWaitFormFieldsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/schema/base-flow-step-wait.zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { FormProvider, useForm } from "react-hook-form";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
}

export default function FlowStepWaitFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepWaitSchema>>({
    resolver: zodResolver(FlowStepWaitSchema),
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
        <FlowStepWaitFormFieldsComponent isDisabled={formMode === "VIEW"} />

        <div className="flex justify-end gap-3 mt-8">
          <Button
            label="Cancel"
            severity="secondary"
            onClick={onCancel}
          />
          <Button
            type="submit"
            label="Save"
            icon="pi pi-check"
            visible={formMode === "ADD" || formMode === "EDIT"}
            disabled={!isValid && (formMode === "ADD" ? false : !isDirty)}
            className="mt-3"
          />
        </div>
      </form>
    </FormProvider>
  );
}
