import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { Button } from "primereact/button";
import { FlowStepCursorDragSchema } from "@/features/flow-step/components/forms/cusror-drag/flow-step-cursor-drag.zod";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import FlowStepCursorDragFormFieldsComponent from "@/features/flow-step/components/forms/cusror-drag/FlowStepCursorDragFormFieldsComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepCursorDragFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepCursorDragSchema>>({
    resolver: zodResolver(FlowStepCursorDragSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  return (
    <div>
      <FormHeaderComponent
        formMode={formMode}
        title="Cursor Drag & Drop Step Configuration"
        description="Click and hold at a source position, drag to a target position, then release. Coordinates can be the result of an Image Search result."
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }),
          )}
          className="flex flex-column h-full"
        >
          <FlowStepCursorDragFormFieldsComponent
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
    </div>
  );
}
