import type z from "zod";

import { FormMode } from "@/shared/enums/form-mode-enum";
import { FormProvider, useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { FlowFormFieldsComponent } from "@/features/flow/components/form/FlowFormFieldsComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { FlowSearchAreaDataTableComponent } from "@/features/flow-search-area/components/FlowSearchAreaDataTableComponent";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
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
    control,
    formState: { isValid, isDirty, errors },
    trigger,
  } = form;

  const { fields, append, remove, update } = useFieldArray<
    z.infer<typeof FlowSchema>,
    "flowSearchAreas"
  >({
    control,
    name: "flowSearchAreas",
  });

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
          <FlowSearchAreaDataTableComponent
            fields={fields}
            append={append}
            remove={remove}
            update={update}
            formMode={formMode}
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
