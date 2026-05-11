import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import FlowStepCursorClickFormFieldsComponent from "@/features/flow-step/components/forms/cusror-click/FlowStepCursorClickFormFieldsComponent";
import { FlowStepCursorClickSchema } from "@/features/flow-step/components/forms/cusror-click/flow-step-cursor-click.zod";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { useEffect } from "react";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepCursorClickFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepCursorClickSchema>>({
    resolver: zodResolver(FlowStepCursorClickSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty },
    trigger,
  } = form;

  // Force full validation on mount so that isValid + errors are in sync
  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  return (
    <>
      <FormHeaderComponent
        title="Cursor Click Step Configuration"
        description="Simulate a left, right, or double mouse click at the specified screen coordinates."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((data) =>
            onSubmit({ ...defaultValues, ...data }),
          )}
          className="flex flex-column h-full"
        >
          <FlowStepCursorClickFormFieldsComponent
            isDisabled={formMode === "VIEW"}
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
