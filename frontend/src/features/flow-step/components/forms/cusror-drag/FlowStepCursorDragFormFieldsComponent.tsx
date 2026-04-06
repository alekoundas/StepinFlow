import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { CursorActionTypeEnum } from "@/shared/enums/backend/cursor-action-type-enum";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepCursorDragFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const cursorOptions = Object.values(CursorActionTypeEnum).map((value) => ({
    label: value.replace("_", " ").toLowerCase(),
    value,
  }));

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormDropdownComponent
        fieldName="cursorActionType"
        labelText="Cursor Action Type"
        mode="local"
        options={cursorOptions}
        optionLabel="label"
        optionValue="value"
        isRequired={true}
        isDisabled={isDisabled}
      />
    </>
  );
}
