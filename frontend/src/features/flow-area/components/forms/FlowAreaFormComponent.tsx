import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FlowAreaZod } from "@/features/flow-area/components/forms/flow-area.zod";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import FlowAreaFormFieldsComponent from "@/features/flow-area/components/forms/FlowAreaFormFieldsComponent";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowAreaDto;
  isFormInDialog?: boolean;
  // Areas this one may sit inside.
  parentOptions: FlowAreaDto[];
  // Regions already inside this one.
  childAreas?: FlowAreaDto[];

  onSubmit: (formValues: FlowAreaDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowAreaFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  parentOptions,
  childAreas,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowAreaZod>>({
    resolver: zodResolver(FlowAreaZod),
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
        title="Search Area"
        description="A reusable region of the screen this flow can search in. Pick a captured area, an application window, or a whole monitor."
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
          onSubmit={form.handleSubmit((partialDto) =>
            onSubmit({ ...defaultValues, ...partialDto } as FlowAreaDto),
          )}
          className="flex flex-column h-full"
        >
          <FlowAreaFormFieldsComponent
            parentOptions={parentOptions}
            childAreas={childAreas}
            isDisabled={formMode === "VIEW"}
          />

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
