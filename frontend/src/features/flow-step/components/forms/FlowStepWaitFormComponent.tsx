import type { FormMode } from "@/shared/enums/form-mode-enum";
import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/FlowStepWaitFormFieldsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/schema/base-flow-step-wait.zod";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { FormProvider, useForm } from "react-hook-form";
import type z from "zod";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto, formMode: FormMode) => void;
}

export default function FlowStepWaitForm({
  formMode,
  defaultValues,
  onSubmit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepWaitSchema>>({
    resolver: zodResolver(FlowStepWaitSchema),
    mode: "onChange",
    defaultValues: defaultValues,
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  // useEffect(() => {
  //   console.log(errors);
  //   console.log(isValid);
  //   console.log(validatingFields);
  // }, [errors, isValid, validatingFields]);
  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit(
          (partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }, formMode), // Merge default values with form values.
        )}
        className="flex flex-column h-full"
      >
        <FlowStepWaitFormFieldsComponent disabled={false} />

        <Button
          type="submit"
          label="Save"
          icon="pi pi-check"
          disabled={!isValid && (formMode === "ADD" ? false : !isDirty)}
          className="mt-3"
        />
      </form>
    </FormProvider>
  );
}
