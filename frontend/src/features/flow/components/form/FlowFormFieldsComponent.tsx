import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputTextAreaComponent } from "@/shared/components/form/FormInputTextAreaComponent";
import { DESCRIPTION_MAX_LENGTH } from "@/features/flow/components/form/flow.zod";

interface Props {
  isDisabled?: boolean;
}

export function FlowFormFieldsComponent({ isDisabled = false }: Props) {
  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormInputTextAreaComponent
        fieldName="description"
        label="Description"
        placeholderText="Logs in and downloads this month's invoices"
        hintText="The list shows the first line, so make it the one that tells two flows apart."
        maxLength={DESCRIPTION_MAX_LENGTH}
        isDisabled={isDisabled}
      />
    </>
  );
}
