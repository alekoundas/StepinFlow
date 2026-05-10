import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FlowSearchAreaZod } from "@/features/flow-search-area/components/forms/flow-search-area.zod";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import FlowSearchAreaFormFieldsComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormFieldsComponent";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowSearchAreaDto;
  isFormInDialog?: boolean;

  onSubmit: (formValues: FlowSearchAreaDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowSearchAreaFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowSearchAreaZod>>({
    resolver: zodResolver(FlowSearchAreaZod),
    mode: "onChange",
    defaultValues: { ...defaultValues },
  });

  const {
    formState: { isValid, isDirty, errors },
  } = form;

  return (
    <div>
      <FormHeaderComponent
        formMode={formMode}
        title="Cursor Drag & Drop Step Configuration"
        description="Click and hold at a source position, drag to a target position, then release. Coordinates can be the result of an Image Search result."
        onEdit={onEdit}
      />
      {/* 
      {Object.keys(errors).length > 0 && (
        <div className="alert alert-danger">
          <strong>Validation Errors:</strong>
          <ul>
            {Object.entries(errors).map(([key, error]) => (
              <li key={key}>{error.message}</li>
            ))}
          </ul>
        </div>
      )} */}

      <FormProvider {...form}>
        <form
          id={formId}
          onSubmit={form.handleSubmit((partialDto: Partial<FlowStepDto>) =>
            onSubmit({ ...defaultValues, ...partialDto }),
          )}
          className="flex flex-column h-full"
        >
          <FlowSearchAreaFormFieldsComponent isDisabled={formMode === "VIEW"} />

          {!isFormInDialog && (
            <FormFooterComponent
              formMode={formMode}
              isValid={isValid}
              isDirty={isDirty}
              onCancel={onCancel}
            />
          )}
        </form>
      </FormProvider>
    </div>
  );
}
