import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FlowStepMarkerSchema } from "@/features/flow-step/components/forms/marker/flow-step-marker.zod";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepMarkerFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepMarkerSchema>>({
    resolver: zodResolver(FlowStepMarkerSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepMarkerSchema>) =>
    onSubmit(new FlowStepDto({ ...defaultValues, ...data }));

  return (
    <>
      <FormHeaderComponent
        title="Marker Step Configuration"
        description="Names the section that follows. Each one becomes a test case in the report, so name it after what a person would say they were doing."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FormInputTextComponent
            fieldName="name"
            label="Name"
            placeholderText="Sign in"
            isRequired={true}
            isDisabled={formMode === "VIEW"}
            className="mt-5"
            hintText="It fails if any step beneath it failed, and passes otherwise."
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
