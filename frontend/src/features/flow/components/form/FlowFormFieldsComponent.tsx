import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";

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

      <FormInputTextComponent
        fieldName="description"
        label="Description"
        placeholderText="Logs in and downloads this month's invoices"
        hintText="Shown in the list. Worth a line, so you can tell two flows apart at a glance."
        isDisabled={isDisabled}
      />
    </>
  );
}
