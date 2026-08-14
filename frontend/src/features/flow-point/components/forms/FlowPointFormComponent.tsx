import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm } from "react-hook-form";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FlowPointZod } from "@/features/flow-point/components/forms/flow-point.zod";
import FlowPointFormFieldsComponent from "@/features/flow-point/components/forms/FlowPointFormFieldsComponent";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowPointDto;
  isFormInDialog?: boolean;
  // Frames this point can be measured from.
  areaOptions: FlowAreaDto[];

  onSubmit: (formValues: FlowPointDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowPointFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  areaOptions,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowPointZod>>({
    resolver: zodResolver(FlowPointZod),
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
            onSubmit({ ...defaultValues, ...partialDto } as FlowPointDto),
          )}
          className="flex flex-column h-full"
        >
          <FlowPointFormFieldsComponent
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
