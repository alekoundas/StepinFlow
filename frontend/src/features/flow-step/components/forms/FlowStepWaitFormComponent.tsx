import FlowStepWaitFormFieldsComponent from "@/features/flow-step/components/forms/FlowStepWaitFormFieldsComponent";
import { FlowStepWaitSchema } from "@/features/flow-step/schema/base-flow-step-wait.zod";
import { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "primereact/button";
import { useEffect } from "react";
import { FormProvider, useForm } from "react-hook-form";
import type z from "zod";

interface Props {
  defaultValues?: FlowStepDto;
  onSubmit: (formValues: Partial<FlowStepDto>) => void;
}

export default function FlowStepWaitForm({ defaultValues, onSubmit }: Props) {
  const form = useForm<z.infer<typeof FlowStepWaitSchema>>({
    resolver: zodResolver(FlowStepWaitSchema),
    mode: "onChange",
    defaultValues: defaultValues,
  });

  const {
    formState: { isValid, isDirty, errors, validatingFields },
  } = form;

  useEffect(() => {
    console.log(errors);
    console.log(isValid);
    console.log(validatingFields);
  }, [errors, isValid, validatingFields]);
  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="flex flex-column h-full"
      >
        <FlowStepWaitFormFieldsComponent disabled={false} />

        <Button
          type="submit"
          label="Save"
          icon="pi pi-check"
          disabled={!isValid && !isDirty}
          className="mt-3"
        />
      </form>
    </FormProvider>
  );
}
