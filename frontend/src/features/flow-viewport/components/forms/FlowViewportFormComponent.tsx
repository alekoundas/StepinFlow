import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowViewportDto } from "@/shared/models/database/flow-viewport-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FlowViewportZod } from "@/features/flow-viewport/components/forms/flow-viewport.zod";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowViewportDto;
  isFormInDialog?: boolean;

  onSubmit: (formValues: FlowViewportDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowViewportFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowViewportZod>>({
    resolver: zodResolver(FlowViewportZod),
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
        title="Screen size"
        description="A size the flow is expected to pass at. The whole flow runs once per size, against the application under test, and each size reports its own result."
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          id={formId}
          onSubmit={form.handleSubmit((partialDto) =>
            onSubmit({ ...defaultValues, ...partialDto } as FlowViewportDto),
          )}
          className="flex flex-column h-full"
        >
          <div className="flex gap-3">
            <FormInputNumberComponent
              fieldName="width"
              label="Width"
              min={1}
              max={2147483647}
              isRequired={true}
              isDisabled={formMode === "VIEW"}
              className="flex-1"
            />

            <FormInputNumberComponent
              fieldName="height"
              label="Height"
              min={1}
              max={2147483647}
              isRequired={true}
              isDisabled={formMode === "VIEW"}
              className="flex-1"
            />
          </div>

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
