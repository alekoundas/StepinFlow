import type z from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";

import { FormMode } from "@/shared/enums/form-mode-enum";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import type { FlowInputDto } from "@/shared/models/database/flow-input-dto";
import { FlowInputZod } from "@/features/flow-input/components/forms/flow-input.zod";

interface Props {
  formId: string;
  formMode: FormMode;
  defaultValues: FlowInputDto;
  // The dialog has its own buttons.
  isFormInDialog?: boolean;

  onSubmit: (formValues: FlowInputDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowInputFormComponent({
  formId,
  formMode,
  defaultValues,
  isFormInDialog = false,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowInputZod>>({
    resolver: zodResolver(FlowInputZod),
    mode: "onChange",
    defaultValues: {
      name: defaultValues.name,
      defaultValue: defaultValues.defaultValue,
      isSecret: defaultValues.isSecret,
      newSecretValue: "",
    },
  });

  const {
    control,
    getValues,
    setValue,
    formState: { isValid, isDirty },
  } = form;

  const name = useWatch({ control, name: "name" });
  const isSecret = useWatch({ control, name: "isSecret" });
  const isDisabled = formMode === FormMode.VIEW;

  // The value goes with it either way, so ticking Secret never makes anyone type it again.
  const changeSecret = (checked: boolean) => {
    const options = { shouldDirty: true, shouldValidate: true };

    if (checked) {
      setValue("newSecretValue", getValues("defaultValue"), options);
      setValue("defaultValue", "", options);
      return;
    }

    setValue("defaultValue", getValues("newSecretValue"), options);
    setValue("newSecretValue", "", options);
  };

  let secretPlaceholder = "Not set yet";
  if (defaultValues.hasSecretValue) secretPlaceholder = "Saved. Type a new one to replace it.";

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
            onSubmit({ ...defaultValues, ...partialDto } as FlowInputDto),
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

          <FormInputCheckboxComponent
            fieldName="isSecret"
            label="Secret"
            hintText="Its value stays in this machine's database: never in the script, the repository or what a model is sent."
            isDisabled={isDisabled}
            onChanged={changeSecret}
          />

          {/* A secret's saved value never comes back, so its field only ever holds a new one. */}
          {isSecret ? (
            <FormInputTextComponent
              fieldName="newSecretValue"
              label="Value"
              placeholderText={secretPlaceholder}
              hintText="Never shown again once saved. Left empty, the saved one stays."
              isDisabled={isDisabled}
              isMasked
            />
          ) : (
            <FormInputTextComponent
              fieldName="defaultValue"
              label="Value"
              hintText="Written into the script. A recording puts what you typed here."
              isDisabled={isDisabled}
            />
          )}

          {!isFormInDialog && (
            <FormFooterComponent
              formMode={formMode}
              isValid={isValid}
              isDirty={formMode === FormMode.ADD || isDirty}
              onCancel={onCancel}
            />
          )}
        </form>
      </FormProvider>
    </div>
  );
}
