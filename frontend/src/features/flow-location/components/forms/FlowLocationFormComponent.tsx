import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FlowLocationZod } from "@/features/flow-location/components/forms/flow-location.zod";
import FlowLocationFormFieldsComponent from "@/features/flow-location/components/forms/FlowLocationFormFieldsComponent";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowLocationDto;
  isFormInDialog?: boolean;
  // Frames this point can be measured from.
  areaOptions: FlowSearchAreaDto[];

  onSubmit: (formValues: FlowLocationDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowLocationFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  areaOptions,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowLocationZod>>({
    resolver: zodResolver(FlowLocationZod),
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
        title="Location"
        description="A named screen point this flow can reuse. Cursor steps point at it instead of storing raw coordinates, so moving the flow to another machine only means re-capturing the location."
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          id={formId}
          onSubmit={form.handleSubmit((partialDto) =>
            onSubmit({ ...defaultValues, ...partialDto } as FlowLocationDto),
          )}
          className="flex flex-column h-full"
        >
          <FlowLocationFormFieldsComponent
            areaOptions={areaOptions}
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
