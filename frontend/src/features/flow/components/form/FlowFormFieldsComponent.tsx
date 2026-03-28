import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
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

      <FormInputNumberComponent
        fieldName="orderNumber"
        label="Order Number"
        min={0}
        max={2147483647}
        isRequired={true}
        isDisabled={isDisabled}
      />
    </>
  );
}
