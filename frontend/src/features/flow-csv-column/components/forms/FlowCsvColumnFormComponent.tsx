import type z from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";

import { FormMode } from "@/shared/enums/form-mode-enum";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import type { FlowCsvColumnDto } from "@/shared/models/database/flow-csv-column-dto";
import { FlowCsvColumnZod } from "@/features/flow-csv-column/components/forms/flow-csv-column.zod";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowCsvColumnDto;

  onSubmit: (formValues: FlowCsvColumnDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowCsvColumnFormComponent({
  formId,
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowCsvColumnZod>>({
    resolver: zodResolver(FlowCsvColumnZod),
    mode: "onChange",
    defaultValues: {
      name: defaultValues.name,
      defaultValue: defaultValues.defaultValue,
      isSecret: defaultValues.isSecret,
    },
  });

  const {
    control,
    setValue,
    formState: { isValid, isDirty },
  } = form;

  const name = useWatch({ control, name: "name" });
  const isSecret = useWatch({ control, name: "isSecret" });
  const isDisabled = formMode === FormMode.VIEW;

  return (
    <div>
      <FormHeaderComponent
        formMode={formMode}
        title="Input"
        description="A value the steps type in, like the email to sign in with."
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          id={formId}
          onSubmit={form.handleSubmit((partialDto) =>
            onSubmit({ ...defaultValues, ...partialDto } as FlowCsvColumnDto),
          )}
          className="flex flex-column h-full mt-4"
        >
          <FormInputTextComponent
            fieldName="name"
            label="Name"
            hintText={`Steps type it where they say {{${name || "name"}}}.`}
            isDisabled={isDisabled}
            isRequired
          />

          {/* Never kept for a secret, so turning it on clears what is there. */}
          <FormInputCheckboxComponent
            fieldName="isSecret"
            label="Secret"
            hintText="Its value is never saved with the flow."
            isDisabled={isDisabled}
            onChanged={(checked) => {
              if (checked) setValue("defaultValue", "", { shouldDirty: true, shouldValidate: true });
            }}
          />

          <FormInputTextComponent
            fieldName="defaultValue"
            label="Default value"
            placeholderText={isSecret ? "Not saved for a secret" : undefined}
            hintText={isSecret ? undefined : "What you typed while recording."}
            isDisabled={isDisabled || isSecret}
          />

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={formMode === FormMode.ADD || isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </div>
  );
}
