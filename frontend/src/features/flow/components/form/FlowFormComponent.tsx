import type z from "zod";

import { FormMode } from "@/shared/enums/form-mode-enum";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { FlowFormFieldsComponent } from "@/features/flow/components/form/FlowFormFieldsComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import FlowSubFlowPanelComponent from "@/features/flow/components/form/FlowSubFlowPanelComponent";
import { useEffect } from "react";

interface Props {
  formMode: FormMode;
  defaultValues: FlowDto;
  onSubmit: (formValues: FlowDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export function FlowFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowSchema>>({
    resolver: zodResolver(FlowSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty, errors },
    trigger,
  } = form;

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);
  useEffect(() => {
    console.log("Form Errors:", errors);
    console.log("Form isValid:", isValid);
    console.log("Form  isDirty:", isDirty);
  }, [isValid, isDirty, errors]);
  return (
    <>
      <FormHeaderComponent
        title="Flow Configuration"
        description="General settings for the flow."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((data) =>
            onSubmit({ ...defaultValues, ...data } as FlowDto),
          )}
          className="flex flex-column h-full"
        >
          <FlowFormFieldsComponent isDisabled={formMode === "VIEW"} />

          <FlowSubFlowPanelComponent
            flowId={defaultValues.id}
            isSubFlow={defaultValues.isSubFlow}
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
