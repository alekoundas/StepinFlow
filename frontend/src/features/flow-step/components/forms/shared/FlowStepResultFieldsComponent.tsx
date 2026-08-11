import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";

interface Props {
  // What the step produces, so the hints can say it in the step's own words.
  resultDescription: string;
  isDisabled?: boolean;
}

/**
 * Naming a step's result is what lets later steps use it as {{name}}. Shared by every step that
 * produces text, so the naming rules stay identical wherever a result comes from.
 */
export default function FlowStepResultFieldsComponent({
  resultDescription,
  isDisabled = false,
}: Props) {
  return (
    <div className="flex gap-3">
      <FormInputTextComponent
        fieldName="resultVariableName"
        label="Save result as"
        placeholderText="windowTitle"
        isDisabled={isDisabled}
        className="flex-1"
        hintText={`Later steps use it as {{name}}. ${resultDescription}`}
      />

      <FormInputTextComponent
        fieldName="resultExtractPattern"
        label="Extract with pattern"
        placeholderText="(\d+)"
        isDisabled={isDisabled}
        className="flex-1"
        hintText="Regex. Keeps the first capture group, or everything when empty."
      />
    </div>
  );
}
