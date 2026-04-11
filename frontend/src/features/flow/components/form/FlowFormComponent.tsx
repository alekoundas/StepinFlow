import type z from "zod";

import { Button } from "primereact/button";
import { FormMode } from "@/shared/enums/form-mode-enum";
import {
  FormProvider,
  useFieldArray,
  useForm,
  type FieldArrayWithId,
} from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { FlowFormFieldsComponent } from "@/features/flow/components/form/FlowFormFieldsComponent";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { FlowSearchAreaDataTableComponent } from "@/features/flow-search-area/components/FlowSearchAreaDataTableComponent";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";

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
    defaultValues: { ...defaultValues },
  });

  const {
    control,
    formState: { isValid, isDirty },
  } = form;

  const { fields, append, remove, update } = useFieldArray<
    z.infer<typeof FlowSchema>,
    "flowSearchAreas",
    "fieldId"
  >({
    control,
    name: "flowSearchAreas",
    keyName: "fieldId",
  });

  return (
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
  );
}
