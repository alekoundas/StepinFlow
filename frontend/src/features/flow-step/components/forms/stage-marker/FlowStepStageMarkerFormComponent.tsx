import type z from "zod";
import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FlowStepStageMarkerSchema } from "@/features/flow-step/components/forms/stage-marker/flow-step-stage-marker.zod";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepStageMarkerFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepStageMarkerSchema>>({
    resolver: zodResolver(FlowStepStageMarkerSchema),
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

  const handleSubmit = (data: z.infer<typeof FlowStepStageMarkerSchema>) =>
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
            <FormInputTextComponent
              fieldName="name"
              label="Name"
              placeholderText="Sign in"
              isRequired={true}
              isDisabled={formMode === "VIEW"}
              hintText="The stage fails if any step in it failed, and passes otherwise."
            />
          </FlowStepFormLayoutComponent>
        </form>
      </FormProvider>
    </>
  );
}
