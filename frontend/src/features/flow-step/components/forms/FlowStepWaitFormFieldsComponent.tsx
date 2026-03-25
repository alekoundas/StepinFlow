import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";

interface Props {
  disabled?: boolean;
}

export default function FlowStepWaitFormFieldsComponent({
  disabled = false,
}: Props) {
  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={disabled}
      />

      <FormInputNumberComponent
        fieldName="waitForMilliseconds"
        label="Wait duration (ms)"
        min={50}
        max={300000}
        isRequired={true}
        isDisabled={disabled}
      />
    </>
  );
}
