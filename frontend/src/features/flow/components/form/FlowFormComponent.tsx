import type z from "zod";

import { Button } from "primereact/button";
import { FormMode } from "@/shared/enums/form-mode-enum";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { FlowSchema } from "@/features/flow/schema/flow-schema.zod";
import { FlowFormFieldsComponent } from "@/features/flow/components/form/FlowFormFieldsComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";

interface Props {
  formMode: FormMode;
  defaultValues: FlowDto;
  onSubmit: (formValues: FlowDto) => void;
  onCancel: () => void;
}

export function FlowFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
}: Props) {
  const form = useForm<z.infer<typeof FlowSchema>>({
    resolver: zodResolver(FlowSchema),
    mode: "onChange",
    defaultValues: defaultValues,
  });

  const {
    formState: { isValid, isDirty },
  } = form;

  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit((partialDto: Partial<FlowDto>) =>
          onSubmit({ ...defaultValues, ...partialDto }),
        )}
        className="flex flex-column h-full"
      >
        <FlowFormFieldsComponent isDisabled={formMode === "VIEW"} />

        <div className="flex justify-end gap-3 mt-8">
          <Button
            label="Cancel"
            severity="secondary"
            onClick={onCancel}
          />
          <Button
            type="submit"
            label="Save"
            icon="pi pi-check"
            visible={formMode === "ADD" || formMode === "EDIT"}
            disabled={!isValid && (formMode === "ADD" ? false : !isDirty)}
            className="mt-3"
          />
        </div>
      </form>
    </FormProvider>
  );
}
